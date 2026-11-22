using System.Text;
using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Application.Services;
using Hms.Domain.Entities;
using Hms.Domain.Enums;
using Hms.Domain.Workflows;
using Hms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hms.Infrastructure.Services;

/// <summary>
/// Pre-procedure clearance lifecycle: Reception/Nurse basic capture, Nurse clinical submission,
/// Doctor decision, then payment. Private payments are individual; HMO payments are bulk from the HMO profile.
/// </summary>
public sealed class FitnessService(
    HmsDbContext db,
    ICurrentUser user,
    IAuditWriter audit,
    INumberGenerator numbers,
    IBillingService billing,
    IFileStorage files) : IFitnessService
{
    public async Task<FitnessCertificateDto> CreateAsync(CreateFitnessCertificateRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.PatientFullName)) throw new AppConflictException("Patient full name is required.");
        if (request.PatientAge is < 0 or > 130) throw new AppConflictException("Patient age is outside a plausible range.");
        if (string.IsNullOrWhiteSpace(request.Phone)) throw new AppConflictException("Patient phone number is required.");
        if (request.PaymentType == FitnessPaymentType.Hmo)
        {
            if (request.HmoCompanyId is null)
                throw new AppConflictException("Select an HMO for an HMO fitness record.");
            if (!await db.HmoCompanies.AnyAsync(x => x.Id == request.HmoCompanyId && x.IsActive, cancellationToken))
                throw new AppConflictException("The selected HMO is not registered or is inactive.");
        }

        var patient = await ResolvePatientAsync(request.PatientNumber, request.PatientFullName, cancellationToken);
        if (request.HmoEnrollmentId is not null)
        {
            var enrollment = await db.PatientHmoEnrollments.SingleOrDefaultAsync(x => x.Id == request.HmoEnrollmentId
                && x.IsActive && (patient == null || x.PatientId == patient.Id), cancellationToken)
                ?? throw new AppConflictException("HMO enrollment is not active for this patient.");
            request = request with { HmoCompanyId = enrollment.HmoCompanyId };
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var certificate = new FitnessCertificate
        {
            CertificateNumber = await numbers.NextAsync("certificate_number_seq", "FIT", cancellationToken),
            PatientId = patient?.Id,
            PatientNumber = patient?.PatientNumber ?? request.PatientNumber,
            PersonFullName = patient?.FullName ?? request.PatientFullName.Trim(),
            PatientAge = request.PatientAge,
            PersonSex = request.PersonSex,
            Phone = request.Phone.Trim(),
            PaymentType = request.PaymentType,
            HmoCompanyId = request.HmoCompanyId,
            HmoEnrollmentId = request.HmoEnrollmentId,
            FitnessDate = request.FitnessDate,
            ExpiresAt = request.ExpiresAt,
            Status = FitnessStatus.AwaitingNurse,
            PaymentStatus = FitnessPaymentStatus.Unpaid,
            IssuedBy = user.RequireId(),
            CreatedBy = user.RequireId()
        };
        db.FitnessCertificates.Add(certificate);
        await db.SaveChangesAsync(cancellationToken);

        await billing.CreateAdministrativeBillAsync(patient?.Id, BillKind.FitnessCertificate,
            FeeCode.FitnessCertificate, certificate.Id, ChargeSourceType.FitnessCertificate,
            $"Fitness clearance {certificate.CertificateNumber}", cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await audit.WriteAsync("fitness.created", nameof(FitnessCertificate), certificate.Id.ToString(), patient?.Id,
            after: new { certificate.CertificateNumber, certificate.PatientNumber, certificate.PersonFullName, certificate.PaymentType }, cancellationToken: cancellationToken);
        return await MapAsync(certificate.Id, cancellationToken);
    }

    public async Task<FitnessCertificateDto> GetAsync(Guid certificateId, CancellationToken cancellationToken = default) =>
        await MapAsync(certificateId, cancellationToken);

    public async Task<FitnessCertificateDto> UpdateClinicalInfoAsync(Guid certificateId, UpdateFitnessClinicalInfoRequest request, CancellationToken cancellationToken = default)
    {
        var certificate = await db.FitnessCertificates.Include(x => x.InvestigationResults)
            .SingleOrDefaultAsync(x => x.Id == certificateId, cancellationToken)
            ?? throw new AppNotFoundException("Fitness record not found.");
        FitnessStateMachine.EnsureCanCaptureClinicalInfo(certificate);
        db.FitnessInvestigationResults.RemoveRange(certificate.InvestigationResults);
        certificate.InvestigationResults.Clear();
        var order = 0;
        foreach (var result in request.InvestigationResults)
        {
            if (string.IsNullOrWhiteSpace(result.Key) || string.IsNullOrWhiteSpace(result.Value)) continue;
            var finding = new FitnessInvestigationResult
            {
                FitnessCertificateId = certificate.Id,
                Key = result.Key.Trim(), Value = result.Value.Trim(), SortOrder = order++
            };
            certificate.InvestigationResults.Add(finding);
            db.FitnessInvestigationResults.Add(finding);
        }
        certificate.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("fitness.clinical-info-updated", nameof(FitnessCertificate), certificate.Id.ToString(),
            patientId: certificate.PatientId, cancellationToken: cancellationToken);
        return await MapAsync(certificate.Id, cancellationToken);
    }

    public async Task<FitnessCertificateDto> UploadCertificateAsync(Guid certificateId, string fileName, string contentType, Stream content, CancellationToken cancellationToken = default)
    {
        var certificate = await db.FitnessCertificates.SingleOrDefaultAsync(x => x.Id == certificateId, cancellationToken)
            ?? throw new AppNotFoundException("Fitness record not found.");
        FitnessStateMachine.EnsureCanCaptureClinicalInfo(certificate);
        var stored = await files.SaveAsync("fitness", certificate.Id, fileName, content, cancellationToken);
        certificate.CertificateFileName = Path.GetFileName(fileName);
        certificate.CertificateContentType = contentType;
        certificate.CertificateStorageKey = stored.StorageKey;
        certificate.CertificateUploadedAt = DateTimeOffset.UtcNow;
        certificate.CertificateUploadedBy = user.RequireId();
        certificate.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("fitness.certificate-uploaded", nameof(FitnessCertificate), certificate.Id.ToString(),
            patientId: certificate.PatientId, cancellationToken: cancellationToken);
        return await MapAsync(certificate.Id, cancellationToken);
    }

    public async Task<FitnessCertificateFileDto> DownloadCertificateAsync(Guid certificateId, CancellationToken cancellationToken = default)
    {
        var certificate = await db.FitnessCertificates.AsNoTracking().SingleOrDefaultAsync(x => x.Id == certificateId, cancellationToken)
            ?? throw new AppNotFoundException("Fitness record not found.");
        if (string.IsNullOrWhiteSpace(certificate.CertificateStorageKey))
            throw new AppNotFoundException("No certificate is attached.");
        var stream = await files.OpenReadAsync(certificate.CertificateStorageKey, cancellationToken);
        return new FitnessCertificateFileDto(certificate.CertificateFileName ?? "fitness-certificate",
            certificate.CertificateContentType ?? "application/octet-stream", stream);
    }

    public async Task<FitnessCertificateDto> SubmitAsync(Guid certificateId, CancellationToken cancellationToken = default)
    {
        var certificate = await db.FitnessCertificates.SingleOrDefaultAsync(x => x.Id == certificateId, cancellationToken)
            ?? throw new AppNotFoundException("Fitness record not found.");
        FitnessStateMachine.EnsureCanSubmit(certificate);
        certificate.Status = FitnessStatus.AwaitingDoctor;
        certificate.SubmittedAt = DateTimeOffset.UtcNow;
        certificate.SubmittedBy = user.RequireId();
        certificate.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("fitness.submitted", nameof(FitnessCertificate), certificate.Id.ToString(),
            patientId: certificate.PatientId, cancellationToken: cancellationToken);
        return await MapAsync(certificate.Id, cancellationToken);
    }

    public async Task<FitnessCertificateDto> DecideAsync(Guid certificateId, FitnessDecisionRequest request, CancellationToken cancellationToken = default)
    {
        var certificate = await db.FitnessCertificates.SingleOrDefaultAsync(x => x.Id == certificateId, cancellationToken)
            ?? throw new AppNotFoundException("Fitness record not found.");
        FitnessStateMachine.EnsureCanDecide(certificate);
        if (!request.Approve && string.IsNullOrWhiteSpace(request.RejectionReason))
            throw new AppConflictException("A rejection reason is required.");
        if (request.Approve)
        {
            certificate.Status = FitnessStatus.Approved;
            certificate.ApprovedAt = DateTimeOffset.UtcNow;
            certificate.ApprovedBy = user.RequireId();
            certificate.RejectionReason = null;
        }
        else
        {
            certificate.Status = FitnessStatus.Rejected;
            certificate.RejectedAt = DateTimeOffset.UtcNow;
            certificate.RejectedBy = user.RequireId();
            certificate.RejectionReason = request.RejectionReason;
        }
        certificate.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync($"fitness.{(request.Approve ? "approved" : "rejected")}", nameof(FitnessCertificate),
            certificate.Id.ToString(), patientId: certificate.PatientId, cancellationToken: cancellationToken);
        return await MapAsync(certificate.Id, cancellationToken);
    }

    public async Task<FitnessCertificateDto> SetPaymentAsync(Guid certificateId, FitnessPaymentRequest request, CancellationToken cancellationToken = default)
    {
        var certificate = await db.FitnessCertificates.SingleOrDefaultAsync(x => x.Id == certificateId, cancellationToken)
            ?? throw new AppNotFoundException("Fitness record not found.");
        FitnessStateMachine.EnsureCanSetPayment(certificate);
        if (certificate.PaymentType == FitnessPaymentType.Hmo)
            throw new AppConflictException("HMO fitness payments must be processed from the HMO profile's Fitness section.");
        certificate.PaymentStatus = request.Paid ? FitnessPaymentStatus.Paid : FitnessPaymentStatus.Unpaid;
        certificate.PaymentUpdatedAt = DateTimeOffset.UtcNow;
        certificate.PaymentUpdatedBy = user.RequireId();
        certificate.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("fitness.payment-updated", nameof(FitnessCertificate), certificate.Id.ToString(),
            patientId: certificate.PatientId, cancellationToken: cancellationToken);
        return await MapAsync(certificate.Id, cancellationToken);
    }

    public async Task<FitnessCertificateDto[]> SetHmoBatchPaymentAsync(FitnessBatchPaymentRequest request, CancellationToken cancellationToken = default)
    {
        var records = await db.FitnessCertificates
            .Where(x => request.CertificateIds.Contains(x.Id) && x.HmoCompanyId == request.HmoCompanyId)
            .ToListAsync(cancellationToken);
        if (records.Count != request.CertificateIds.Distinct().Count())
            throw new AppNotFoundException("One or more HMO fitness records were not found.");
        foreach (var record in records)
        {
            FitnessStateMachine.EnsureCanSetPayment(record);
            if (record.PaymentType != FitnessPaymentType.Hmo)
                throw new AppConflictException("Only HMO fitness records can be bulk-paid.");
            record.PaymentStatus = request.Paid ? FitnessPaymentStatus.Paid : FitnessPaymentStatus.Unpaid;
            record.PaymentUpdatedAt = DateTimeOffset.UtcNow;
            record.PaymentUpdatedBy = user.RequireId();
            record.UpdatedAt = DateTimeOffset.UtcNow;
        }
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("fitness.hmo-batch-payment", nameof(FitnessCertificate), request.HmoCompanyId.ToString(),
            after: new { request.CertificateIds, request.Paid }, cancellationToken: cancellationToken);
        return records.Select(MapSummary).ToArray();
    }

    public Task<FitnessCertificateDto> MarkPaidAsync(Guid certificateId, CancellationToken cancellationToken = default) =>
        SetPaymentAsync(certificateId, new FitnessPaymentRequest(true), cancellationToken);

    public async Task<PagedResult<FitnessCertificateDto>> SearchAsync(DateOnly? from, DateOnly? to, Guid? hmoCompanyId, string? search, int page, int pageSize,
        FitnessStatus? status = null, FitnessPaymentType? paymentType = null, FitnessPaymentStatus? paymentStatus = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.FitnessCertificates.AsNoTracking()
            .Include(x => x.Patient).Include(x => x.HmoCompany).Include(x => x.InvestigationResults)
            .AsQueryable();
        if (from is not null) query = query.Where(x => x.FitnessDate >= from);
        if (to is not null) query = query.Where(x => x.FitnessDate <= to);
        if (hmoCompanyId is not null) query = query.Where(x => x.HmoCompanyId == hmoCompanyId);
        if (status is not null) query = query.Where(x => x.Status == status);
        if (paymentType is not null) query = query.Where(x => x.PaymentType == paymentType);
        if (paymentStatus is not null) query = query.Where(x => x.PaymentStatus == paymentStatus);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.PersonFullName, term)
                || (x.PatientNumber != null && EF.Functions.ILike(x.PatientNumber, term))
                || EF.Functions.ILike(x.CertificateNumber, term)
                || EF.Functions.ILike(x.Phone, term));
        }
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderByDescending(x => x.FitnessDate).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var ids = rows.Select(x => x.Id).ToList();
        var bills = await db.Bills.AsNoTracking()
            .Where(x => x.Kind == BillKind.FitnessCertificate && x.FitnessCertificateId != null && ids.Contains(x.FitnessCertificateId.Value) && x.Status != BillStatus.Void)
            .ToDictionaryAsync(x => x.FitnessCertificateId!.Value, x => x.Id, cancellationToken);
        return new PagedResult<FitnessCertificateDto>(rows.Select(x => Map(x, bills.GetValueOrDefault(x.Id))).ToList(), page, pageSize, total);
    }

    public async Task<IReadOnlyList<FitnessCertificateDto>> GetForPatientAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var rows = await db.FitnessCertificates.AsNoTracking()
            .Include(x => x.Patient).Include(x => x.HmoCompany).Include(x => x.InvestigationResults)
            .Where(x => x.PatientId == patientId)
            .OrderByDescending(x => x.FitnessDate).ToListAsync(cancellationToken);
        var ids = rows.Select(x => x.Id).ToList();
        var bills = await db.Bills.AsNoTracking()
            .Where(x => x.Kind == BillKind.FitnessCertificate && x.FitnessCertificateId != null && ids.Contains(x.FitnessCertificateId.Value) && x.Status != BillStatus.Void)
            .ToDictionaryAsync(x => x.FitnessCertificateId!.Value, x => x.Id, cancellationToken);
        return rows.Select(x => Map(x, bills.GetValueOrDefault(x.Id))).ToList();
    }

    public async Task<byte[]> ExportCsvAsync(DateOnly? from, DateOnly? to, Guid? hmoCompanyId, string? search,
        FitnessStatus? status = null, FitnessPaymentType? paymentType = null, FitnessPaymentStatus? paymentStatus = null,
        CancellationToken cancellationToken = default)
    {
        var result = await SearchAsync(from, to, hmoCompanyId, search, 1, 10000, status, paymentType, paymentStatus, cancellationToken);
        var sb = new StringBuilder();
        sb.AppendLine("CertificateNumber,PatientNumber,Patient,Age,Sex,Phone,PaymentType,FitnessDate,ExpiresAt,HMO,Status,PaymentStatus,CertificateFile");
        foreach (var row in result.Items)
        {
            sb.AppendLine(string.Join(',', Csv(row.CertificateNumber), Csv(row.PatientNumber), Csv(row.PatientName), row.PatientAge,
                row.PersonSex, Csv(row.Phone), row.PaymentType, row.FitnessDate.ToString("yyyy-MM-dd"),
                row.ExpiresAt?.ToString("yyyy-MM-dd") ?? "", Csv(row.HmoCompanyName), row.Status, row.PaymentStatus,
                Csv(row.CertificateFileName)));
        }
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private async Task<Patient?> ResolvePatientAsync(string? patientNumber, string fullName, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(patientNumber))
        {
            var match = await db.Patients.SingleOrDefaultAsync(x => x.PatientNumber == patientNumber.Trim(), cancellationToken);
            // Unknown numbers are kept as free-text references; a registered patient is not required.
            return match;
        }
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return null;
        var first = parts[0];
        var last = parts[^1];
        var matches = await db.Patients.AsNoTracking()
            .Where(x => EF.Functions.ILike(x.FirstName, first) && EF.Functions.ILike(x.LastName, last))
            .Take(2).ToListAsync(cancellationToken);
        if (matches.Count > 1) throw new AppConflictException("Multiple patients match this name. Enter the patient number to link a record, or leave it blank for a free-entry certificate.");
        return matches.SingleOrDefault();
    }

    private async Task<FitnessCertificateDto> MapAsync(Guid id, CancellationToken cancellationToken)
    {
        var certificate = await db.FitnessCertificates.AsNoTracking()
            .Include(x => x.Patient).Include(x => x.HmoCompany).Include(x => x.InvestigationResults)
            .SingleAsync(x => x.Id == id, cancellationToken);
        var billId = await db.Bills.AsNoTracking()
            .Where(x => x.Kind == BillKind.FitnessCertificate && x.FitnessCertificateId == id && x.Status != BillStatus.Void)
            .Select(x => (Guid?)x.Id).FirstOrDefaultAsync(cancellationToken);
        return Map(certificate, billId);
    }

    private static FitnessCertificateDto Map(FitnessCertificate x, Guid? billId) => new(
        x.Id, x.CertificateNumber, x.PatientId, x.PatientNumber, x.Patient?.FullName ?? x.PersonFullName,
        x.PatientAge, x.PersonSex, x.Phone, x.PaymentType, x.FitnessDate, x.ExpiresAt, x.HmoCompanyId, x.HmoCompany?.Name,
        x.Status, x.PaymentStatus, x.RejectionReason, x.CertificateFileName, x.CertificateContentType,
        x.SubmittedAt, x.ApprovedAt, x.PaymentUpdatedAt, billId,
        x.InvestigationResults.OrderBy(r => r.SortOrder).Select(r => new FitnessInvestigationResultDto(r.Key, r.Value)).ToList());

    private static FitnessCertificateDto MapSummary(FitnessCertificate x) => Map(x, null);

    private static string Csv(string? value)
    {
        var text = value ?? string.Empty;
        return text.Contains(',') || text.Contains('"') || text.Contains('\n')
            ? $"\"{text.Replace("\"", "\"\"")}\""
            : text;
    }
}
