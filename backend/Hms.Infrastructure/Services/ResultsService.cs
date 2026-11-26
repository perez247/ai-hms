using System.Text.Json;
using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Application.Services;
using Hms.Domain.Entities;
using Hms.Domain.Enums;
using Hms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hms.Infrastructure.Services;

/// <summary>Lab and procedure request queues and result entry, independent of encounter sign-off.</summary>
public sealed class ResultsService(HmsDbContext db, ICurrentUser user, IAuditWriter audit, IBillingService billing) : IResultsService
{
    public async Task<IReadOnlyList<ServiceRequestDto>> ListAsync(StockItemType kind, ServiceRequestStatus? status,
        string? search, CancellationToken cancellationToken = default)
    {
        var term = (search ?? string.Empty).Trim().ToLowerInvariant();
        var query = db.ServiceRequests.AsNoTracking()
            .Include(x => x.Encounter).Include(x => x.Patient).Include(x => x.StockItem)
            .Where(x => x.Kind == kind);
        if (status is not null) query = query.Where(x => x.Status == status.Value);
        if (term.Length > 0) query = query.Where(x => x.RequestNumber.ToLower().Contains(term)
            || x.Patient.PatientNumber.ToLower().Contains(term) || x.Patient.FullName.ToLower().Contains(term)
            || x.StockItem.Name.ToLower().Contains(term));
        var rows = await query.OrderByDescending(x => x.OrderedAt).Take(200).ToListAsync(cancellationToken);
        var doctorIds = rows.Select(x => x.DoctorId).Distinct().ToList();
        var doctors = await db.Users.AsNoTracking().Where(x => doctorIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.FullName, cancellationToken);
        var ids = rows.Select(x => x.Id).ToList();
        var results = await db.ServiceResults.AsNoTracking().Where(x => ids.Contains(x.RequestId))
            .ToDictionaryAsync(x => x.RequestId, cancellationToken);
        return rows.Select(x => Map(x, results.GetValueOrDefault(x.Id), doctors.GetValueOrDefault(x.DoctorId, "Unknown"))).ToList();
    }

    public async Task<ServiceRequestDto> GetAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        var request = await db.ServiceRequests.Include(x => x.Encounter).Include(x => x.Patient).Include(x => x.StockItem)
            .SingleOrDefaultAsync(x => x.Id == requestId, cancellationToken)
            ?? throw new AppNotFoundException("Service request not found.");
        var result = await db.ServiceResults.AsNoTracking().SingleOrDefaultAsync(x => x.RequestId == requestId, cancellationToken);
        var doctor = await db.Users.AsNoTracking().Where(x => x.Id == request.DoctorId)
            .Select(x => x.FullName).FirstOrDefaultAsync(cancellationToken) ?? "Unknown";
        return Map(request, result, doctor);
    }

    public async Task<ServiceRequestDto> SaveAsync(Guid requestId, SaveServiceResultRequest request, CancellationToken cancellationToken = default)
    {
        var (serviceRequest, result) = await RequireAsync(requestId, cancellationToken);
        EnsureEditable(result);
        var before = Snapshot(result);
        result.ResultText = request.ResultText;
        result.Findings = request.Findings;
        result.StructuredJson = request.StructuredJson;
        result.AbnormalFlag = request.AbnormalFlag;
        if (result.Status == ServiceResultStatus.Pending) result.Status = ServiceResultStatus.Entered;
        result.EnteredById = user.RequireId();
        result.EnteredAt = DateTimeOffset.UtcNow;
        result.Version++;
        result.UpdatedAt = DateTimeOffset.UtcNow;
        AddRevision(result, before, request.Reason);
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("service-result.saved", nameof(ServiceResult), result.Id.ToString(),
            serviceRequest.PatientId, serviceRequest.EncounterId, after: new { result.Status, result.Version }, cancellationToken: cancellationToken);
        return await GetAsync(requestId, cancellationToken);
    }

    public async Task<ServiceRequestDto> SubmitAsync(Guid requestId, SubmitServiceResultRequest request, CancellationToken cancellationToken = default)
    {
        var (serviceRequest, result) = await RequireAsync(requestId, cancellationToken);
        EnsureEditable(result);
        if (string.IsNullOrWhiteSpace(request.ResultText) && string.IsNullOrWhiteSpace(request.Findings))
            throw new AppConflictException("A result or finding is required before submission.");
        var before = Snapshot(result);
        result.ResultText = request.ResultText;
        result.Findings = request.Findings;
        result.StructuredJson = request.StructuredJson;
        result.AbnormalFlag = request.AbnormalFlag;
        result.PerformedAt = request.PerformedAt ?? DateTimeOffset.UtcNow;
        result.Status = ServiceResultStatus.Submitted;
        result.SubmittedById = user.RequireId();
        result.SubmittedAt = DateTimeOffset.UtcNow;
        result.EnteredById = user.RequireId();
        result.EnteredAt = DateTimeOffset.UtcNow;
        result.Version++;
        result.UpdatedAt = DateTimeOffset.UtcNow;
        AddRevision(result, before, null);

        serviceRequest.Status = ServiceRequestStatus.Completed;
        serviceRequest.PerformedAt = result.PerformedAt;
        serviceRequest.UpdatedAt = DateTimeOffset.UtcNow;
        if (serviceRequest.BillLineCreatedAt is null)
        {
            var bill = await billing.GetOrCreateVisitBillAsync(serviceRequest.Encounter!, cancellationToken);
            var category = serviceRequest.Kind == StockItemType.Test ? BillLineCategory.Investigation : BillLineCategory.Surgery;
            var source = serviceRequest.Kind == StockItemType.Test ? ChargeSourceType.InvestigationOrder : ChargeSourceType.SurgeryCase;
            await billing.AddChargeAsync(bill.Id, category, serviceRequest.StockItem!.Name, 1m,
                serviceRequest.UnitPriceSnapshot, source, serviceRequest.Id,
                $"service-request:{serviceRequest.Id}", serviceRequest.EncounterId,
                serviceRequest.Encounter.AdmissionId, serviceRequest.StockItem.TariffCode, cancellationToken);
            serviceRequest.BillLineCreatedAt = DateTimeOffset.UtcNow;
        }
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("service-result.submitted", nameof(ServiceResult), result.Id.ToString(),
            serviceRequest.PatientId, serviceRequest.EncounterId, after: new { result.Status, result.Version }, cancellationToken: cancellationToken);
        return await GetAsync(requestId, cancellationToken);
    }

    private async Task<(ServiceRequest Request, ServiceResult Result)> RequireAsync(Guid requestId, CancellationToken cancellationToken)
    {
        var request = await db.ServiceRequests.Include(x => x.Encounter).Include(x => x.StockItem)
            .SingleOrDefaultAsync(x => x.Id == requestId, cancellationToken)
            ?? throw new AppNotFoundException("Service request not found.");
        var result = await db.ServiceResults.SingleOrDefaultAsync(x => x.RequestId == requestId, cancellationToken)
            ?? throw new AppNotFoundException("Result record not found for this request.");
        if (request.Status == ServiceRequestStatus.Cancelled)
            throw new AppConflictException("Cancelled requests cannot be updated.");
        return (request, result);
    }

    /// <summary>Submitted results are final; no draft edit or resubmission is accepted.</summary>
    private static void EnsureEditable(ServiceResult result)
    {
        if (result.Status is ServiceResultStatus.Submitted or ServiceResultStatus.Amended)
            throw new AppConflictException("This result has been submitted and is locked.");
    }

    private void AddRevision(ServiceResult result, string before, string? reason)
    {
        db.ServiceResultRevisions.Add(new ServiceResultRevision
        {
            ResultId = result.Id, Version = result.Version, BeforeJson = before, AfterJson = Snapshot(result),
            ActorId = user.Id, ActorRole = user.Role?.ToString(), Reason = reason
        });
    }

    private static string Snapshot(ServiceResult result) => JsonSerializer.Serialize(new
    {
        result.Status, result.ResultText, result.Findings, result.StructuredJson, result.AbnormalFlag, result.Version
    });

    private static ServiceRequestDto Map(ServiceRequest request, ServiceResult? result, string doctorName) => new(
        request.Id, request.RequestNumber, request.EncounterId, request.Encounter?.EncounterNumber ?? string.Empty,
        request.PatientId, request.Patient?.PatientNumber ?? string.Empty, request.Patient?.FullName ?? "Unknown",
        request.StockItemId, request.StockItem?.Name ?? "Item", request.Kind, request.Status, request.OrderedAt,
        doctorName, request.FundingSource, null, request.Priority, request.ClinicalNote,
        result?.Id, result?.Status, result?.ResultText, result?.Findings, result?.AbnormalFlag, result?.Version ?? 0);
}
