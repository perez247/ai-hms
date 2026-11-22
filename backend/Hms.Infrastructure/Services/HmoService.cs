using Hms.Application.Common;
using Hms.Infrastructure.Persistence;
using Hms.Application.DTOs;
using Hms.Application.Services;
using Hms.Domain.Billing;
using Hms.Domain.Entities;
using Hms.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Hms.Infrastructure.Services;

/// <summary>PA capture, line coverage, and HMO approval separate from Finance approval.</summary>
public sealed class HmoService(
    HmsDbContext db,
    ICurrentUser user,
    IAuditWriter audit,
    IBillingService billing) : IHmoService
{
    public async Task<PriorAuthorizationDto> SavePriorAuthorizationAsync(SavePriorAuthorizationRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.PaCode)) throw new AppConflictException("PA code is required.");
        if (request.EncounterId is null && request.AdmissionId is null)
            throw new AppConflictException("A PA must be linked to an encounter or admission.");

        var enrollment = await db.PatientHmoEnrollments.Include(x => x.HmoCompany)
            .SingleOrDefaultAsync(x => x.Id == request.HmoEnrollmentId && x.HmoCompanyId == request.HmoCompanyId && x.IsActive, cancellationToken)
            ?? throw new AppConflictException("HMO enrollment is invalid or inactive.");

        if (request.EncounterId is not null)
        {
            var valid = await db.Encounters.AnyAsync(x => x.Id == request.EncounterId && x.PatientId == enrollment.PatientId && x.HmoEnrollmentId == enrollment.Id, cancellationToken);
            if (!valid) throw new AppConflictException("Encounter does not belong to this patient and HMO enrollment.");
        }
        if (request.AdmissionId is not null)
        {
            var valid = await db.Admissions.AnyAsync(x => x.Id == request.AdmissionId && x.PatientId == enrollment.PatientId && x.HmoEnrollmentId == enrollment.Id, cancellationToken);
            if (!valid) throw new AppConflictException("Admission does not belong to this patient and HMO enrollment.");
        }

        var pa = new PriorAuthorization
        {
            PatientId = enrollment.PatientId,
            HmoCompanyId = request.HmoCompanyId,
            HmoEnrollmentId = request.HmoEnrollmentId,
            EncounterId = request.EncounterId,
            AdmissionId = request.AdmissionId,
            PaCode = request.PaCode.Trim(),
            Status = PriorAuthorizationStatus.Issued,
            IssuedAt = DateTimeOffset.UtcNow,
            ExpiresAt = request.ExpiresAt,
            ApprovedAmount = request.ApprovedAmount,
            Notes = request.Notes,
            RequestedBy = user.RequireId()
        };
        db.PriorAuthorizations.Add(pa);
        await db.SaveChangesAsync(cancellationToken);

        var bill = await db.Bills
            .Where(x => x.Status != BillStatus.Void && x.PayerType == PayerType.Hmo
                && ((request.EncounterId != null && x.EncounterId == request.EncounterId)
                    || (request.AdmissionId != null && x.AdmissionId == request.AdmissionId)))
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (bill is not null)
        {
            bill.PriorAuthorizationId = pa.Id;
            await db.SaveChangesAsync(cancellationToken);
        }

        await audit.WriteAsync("hmo.prior-authorization-issued", nameof(PriorAuthorization), pa.Id.ToString(), pa.PatientId, pa.EncounterId, bill?.Id,
            after: new { pa.PaCode, pa.ApprovedAmount, pa.ExpiresAt }, cancellationToken: cancellationToken);
        return Map(pa, enrollment.HmoCompany?.Name ?? "HMO");
    }

    public async Task<BillDto> UpdateCoverageAsync(Guid billId, UpdateHmoCoverageRequest request, CancellationToken cancellationToken = default)
    {
        var bill = await db.Bills.Include(x => x.Lines)
            .SingleOrDefaultAsync(x => x.Id == billId, cancellationToken)
            ?? throw new AppNotFoundException("Bill not found.");
        if (bill.PayerType != PayerType.Hmo) throw new AppConflictException("Coverage updates apply only to HMO bills.");
        if (bill.Status is BillStatus.Approved or BillStatus.Paid or BillStatus.Void)
            throw new AppConflictException("Coverage cannot be changed after Finance approval.");

        foreach (var update in request.Lines)
        {
            var line = bill.Lines.SingleOrDefault(x => x.Id == update.BillLineId)
                ?? throw new AppConflictException("A line does not belong to this bill.");
            if (update.HmoAmount < 0 || update.HmoAmount > line.NetAmount)
                throw new AppConflictException("HMO amount must be between zero and the line net amount.");
            line.IsHmoCovered = update.IsCovered;
            line.HmoAmount = update.IsCovered ? update.HmoAmount : 0m;
            line.PatientAmount = line.NetAmount - line.HmoAmount;
            line.ReferralDestination = update.ReferralDestination;
            line.HmoCoverageReviewedAt = DateTimeOffset.UtcNow;
            line.HmoCoverageReviewedBy = user.RequireId();
        }

        BillingCalculator.Recalculate(bill);
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("hmo.coverage-updated", nameof(Bill), bill.Id.ToString(), bill.PatientId, bill.EncounterId, bill.Id,
            after: new { bill.HmoPayableTotal, bill.PatientPayableTotal }, cancellationToken: cancellationToken);
        return await billing.GetAsync(bill.Id, cancellationToken);
    }

    /// <summary>HMO approval gate; Finance approval remains mandatory for HMO payment.</summary>
    public async Task<Bill> ApproveAsync(Guid billId, CancellationToken cancellationToken = default)
    {
        var bill = await db.Bills
            .Include(x => x.Lines)
            .Include(x => x.PriorAuthorization)
            .Include(x => x.Adjustments)
            .Include(x => x.Payments)
            .Include(x => x.Receivable)
            .SingleOrDefaultAsync(x => x.Id == billId, cancellationToken)
            ?? throw new AppNotFoundException("Bill not found.");
        if (bill.PayerType != PayerType.Hmo) throw new AppConflictException("Only HMO bills can receive HMO approval.");
        if (bill.Status != BillStatus.HmoPending) throw new AppConflictException("Bill must be routed to the HMO Office first.");
        var pa = bill.PriorAuthorization ?? throw new AppConflictException("A prior authorization is required before HMO approval.");
        if (pa.Status != PriorAuthorizationStatus.Issued) throw new AppConflictException("The prior authorization is not issued.");
        if (pa.ExpiresAt is not null && pa.ExpiresAt < DateTimeOffset.UtcNow) throw new AppConflictException("The prior authorization has expired.");

        foreach (var line in bill.Lines.Where(x => x.Category == BillLineCategory.Medication))
        {
            if (line.TariffConfirmedAt is null && bill.HmoPayableTotal > 0)
                throw new AppConflictException("Pharmacy must confirm the HMO tariff before HMO approval.");
            line.HmoCoverageReviewedAt ??= DateTimeOffset.UtcNow;
            line.HmoCoverageReviewedBy ??= user.RequireId();
        }

        BillingCalculator.Recalculate(bill);
        bill.HmoApprovedAt = DateTimeOffset.UtcNow;
        bill.HmoApprovedBy = user.RequireId();
        BillStateMachine.EnsureTransition(bill, BillStatus.HmoApproved);
        bill.Status = BillStatus.HmoApproved;
        bill.UpdatedAt = DateTimeOffset.UtcNow;
        db.BillStatusHistories.Add(new BillStatusHistory
        {
            BillId = bill.Id,
            FromStatus = BillStatus.HmoPending,
            ToStatus = BillStatus.HmoApproved,
            ActorId = user.Id,
            Reason = $"HMO approved against PA {pa.PaCode}"
        });
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("hmo.bill-approved", nameof(Bill), bill.Id.ToString(), bill.PatientId, bill.EncounterId, bill.Id,
            after: new { pa.PaCode, bill.HmoPayableTotal }, cancellationToken: cancellationToken);
        return bill;
    }

    public async Task<PagedResult<PriorAuthorizationDto>> SearchAuthorizationsAsync(Guid? patientId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.PriorAuthorizations.AsNoTracking().Include(x => x.HmoCompany).AsQueryable();
        if (patientId is not null) query = query.Where(x => x.PatientId == patientId);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderByDescending(x => x.RequestedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResult<PriorAuthorizationDto>(rows.Select(x => Map(x, x.HmoCompany?.Name ?? "HMO")).ToList(), page, pageSize, total);
    }

    public async Task<IReadOnlyList<HmoDirectoryEntryDto>> GetDirectoryAsync(CancellationToken cancellationToken = default)
    {
        var companies = await db.HmoCompanies.AsNoTracking()
            .Include(x => x.Tariffs).ThenInclude(x => x.Items)
            .OrderBy(x => x.Name).ToListAsync(cancellationToken);
        return companies.Select(MapDirectory).ToList();
    }

    public async Task<HmoDirectoryEntryDto> RegisterAsync(RegisterHmoRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
            throw new AppConflictException("HMO code and name are required.");
        var code = request.Code.Trim().ToUpperInvariant();
        if (await db.HmoCompanies.AnyAsync(x => x.Code == code, cancellationToken))
            throw new AppConflictException($"An HMO with code {code} already exists.");

        var company = new HmoCompany
        {
            Code = code,
            Name = request.Name.Trim(),
            ContactPerson = request.ContactPerson,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address,
            ContractTerms = request.ContractTerms,
            IsActive = true,
            CreatedBy = user.RequireId()
        };
        db.HmoCompanies.Add(company);
        await db.SaveChangesAsync(cancellationToken);

        if (request.InitialTariff is not null)
            await SaveTariffAsync(company.Id, request.InitialTariff, cancellationToken);

        await audit.WriteAsync("hmo.registered", nameof(HmoCompany), company.Id.ToString(),
            after: new { company.Code, company.Name }, cancellationToken: cancellationToken);
        return await GetDirectoryEntryAsync(company.Id, cancellationToken);
    }

    public async Task<HmoTariffDto> SaveTariffAsync(Guid hmoCompanyId, CreateHmoTariffRequest request, CancellationToken cancellationToken = default)
    {
        if (!await db.HmoCompanies.AnyAsync(x => x.Id == hmoCompanyId, cancellationToken))
            throw new AppNotFoundException("HMO not found.");
        if (string.IsNullOrWhiteSpace(request.Name)) throw new AppConflictException("Tariff name is required.");
        if (request.Items.Count == 0) throw new AppConflictException("Add at least one medication price to the tariff.");
        if (request.Items.Any(x => x.UnitPrice < 0 || x.CoveragePercent is < 0 or > 100))
            throw new AppConflictException("Tariff prices cannot be negative and coverage must be between 0 and 100 percent.");
        if (request.EffectiveTo is not null && request.EffectiveTo < request.EffectiveFrom)
            throw new AppConflictException("Tariff end date cannot precede its start date.");

        var active = await db.HmoTariffs.Where(x => x.HmoCompanyId == hmoCompanyId && x.Status == TariffStatus.Active)
            .ToListAsync(cancellationToken);
        foreach (var previous in active)
        {
            previous.Status = TariffStatus.Superseded;
            previous.EffectiveTo ??= request.EffectiveFrom.AddDays(-1);
        }
        var version = await db.HmoTariffs.Where(x => x.HmoCompanyId == hmoCompanyId).Select(x => (int?)x.Version).MaxAsync(cancellationToken) ?? 0;
        var tariff = new HmoTariff
        {
            HmoCompanyId = hmoCompanyId,
            Name = request.Name.Trim(),
            Version = version + 1,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            Status = TariffStatus.Active,
            ImportedBy = user.RequireId()
        };
        foreach (var item in request.Items)
        {
            if (string.IsNullOrWhiteSpace(item.Code)) continue;
            tariff.Items.Add(new HmoTariffItem
            {
                Code = item.Code.Trim(),
                Description = string.IsNullOrWhiteSpace(item.Description) ? item.Code.Trim() : item.Description.Trim(),
                Category = item.Category,
                Unit = item.Unit,
                UnitPrice = item.UnitPrice,
                CoveragePercent = item.CoveragePercent,
                RequiresPriorAuthorization = item.RequiresPriorAuthorization
            });
        }
        db.HmoTariffs.Add(tariff);
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("hmo.tariff-saved", nameof(HmoTariff), tariff.Id.ToString(),
            after: new { tariff.HmoCompanyId, tariff.Version, ItemCount = tariff.Items.Count }, cancellationToken: cancellationToken);
        return MapTariff(tariff);
    }

    private async Task<HmoDirectoryEntryDto> GetDirectoryEntryAsync(Guid id, CancellationToken cancellationToken)
    {
        var company = await db.HmoCompanies.AsNoTracking().Include(x => x.Tariffs).ThenInclude(x => x.Items)
            .SingleAsync(x => x.Id == id, cancellationToken);
        return MapDirectory(company);
    }

    private static HmoDirectoryEntryDto MapDirectory(HmoCompany company) => new(
        company.Id, company.Code, company.Name, company.IsActive,
        company.Tariffs.OrderByDescending(x => x.Version).Select(MapTariff).ToList());

    private static HmoTariffDto MapTariff(HmoTariff tariff) => new(
        tariff.Id, tariff.HmoCompanyId, tariff.Name, tariff.Version, tariff.EffectiveFrom, tariff.EffectiveTo, tariff.Status,
        tariff.Items.Where(x => x.IsActive).OrderBy(x => x.Code)
            .Select(x => new HmoTariffItemDto(x.Id, x.Code, x.Description, x.Category, x.Unit,
                x.UnitPrice, x.CoveragePercent, x.RequiresPriorAuthorization, x.IsActive)).ToList());

    private static PriorAuthorizationDto Map(PriorAuthorization pa, string hmoName) => new(
        pa.Id, pa.PatientId, pa.HmoCompanyId, hmoName, pa.EncounterId, pa.AdmissionId, pa.PaCode,
        pa.Status, pa.RequestedAt, pa.IssuedAt, pa.ExpiresAt, pa.ApprovedAmount, pa.Notes);
}
