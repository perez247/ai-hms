using Hms.Application.Common;
using Hms.Infrastructure.Persistence;
using Hms.Application.DTOs;
using Hms.Application.Services;
using Hms.Domain.Billing;
using Hms.Domain.Entities;
using Hms.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Hms.Infrastructure.Services;

/// <summary>Billing aggregate orchestration: create bills, resolve tariff pricing, add idempotent charge lines, commit and recalculate totals.</summary>
public sealed partial class BillingService(
    HmsDbContext db,
    ICurrentUser user,
    IAuditWriter audit,
    INumberGenerator numbers,
    ITariffPricingService tariffs) : IBillingService
{
    /// <summary>
    /// Finance-created bill. When an HMO is selected, medication lines are priced from the HMO's
    /// active negotiated tariff; lines without a matched tariff fall back to the market price and
    /// are reported as undercovered so Finance can see the gap.
    /// </summary>
    public async Task<ManualBillResultDto> CreateManualBillAsync(CreateManualBillRequest request, CancellationToken cancellationToken = default)
    {
        var patient = await db.Patients.SingleOrDefaultAsync(x => x.Id == request.PatientId, cancellationToken)
            ?? throw new AppNotFoundException("Patient not found.");
        if (request.Lines.Count == 0) throw new AppConflictException("Add at least one bill line.");
        if (request.Lines.Any(x => x.Quantity <= 0 || x.UnitPrice < 0))
            throw new AppConflictException("Line quantity must be positive and unit price cannot be negative.");

        PatientHmoEnrollment? enrollment = null;
        Guid? hmoCompanyId = null;
        if (request.HmoEnrollmentId is not null)
        {
            enrollment = await db.PatientHmoEnrollments.Include(x => x.HmoCompany)
                .SingleOrDefaultAsync(x => x.Id == request.HmoEnrollmentId && x.PatientId == patient.Id && x.IsActive, cancellationToken)
                ?? throw new AppConflictException("The selected HMO enrollment is not active for this patient.");
            hmoCompanyId = enrollment.HmoCompanyId;
            if (request.HmoCompanyId is not null && request.HmoCompanyId != hmoCompanyId)
                throw new AppConflictException("The selected HMO does not match the enrollment.");
        }
        else if (request.HmoCompanyId is not null)
        {
            var hmo = await db.HmoCompanies.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == request.HmoCompanyId && x.IsActive, cancellationToken)
                ?? throw new AppConflictException("The selected HMO is not registered or is inactive.");
            hmoCompanyId = hmo.Id;
            enrollment = await db.PatientHmoEnrollments.Include(x => x.HmoCompany)
                .Where(x => x.PatientId == patient.Id && x.HmoCompanyId == hmo.Id && x.IsActive)
                .OrderByDescending(x => x.IsPrimary).FirstOrDefaultAsync(cancellationToken);
        }

        var priceBook = hmoCompanyId is null
            ? new Dictionary<string, HmoTariffItem>()
            : await tariffs.ResolveAsync(hmoCompanyId.Value, cancellationToken);
        var warnings = new List<string>();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var bill = new Bill
        {
            BillNumber = await numbers.NextAsync("bill_number_seq", "BILL", cancellationToken),
            PatientId = patient.Id,
            Kind = BillKind.Manual,
            PayerType = hmoCompanyId is null ? PayerType.SelfPay : PayerType.Hmo,
            HmoCompanyId = hmoCompanyId,
            HmoEnrollmentId = enrollment?.Id,
            Status = BillStatus.Committed,
            CommittedAt = DateTimeOffset.UtcNow,
            CommittedBy = user.RequireId(),
            CreatedBy = user.RequireId(),
            Currency = "NGN"
        };

        foreach (var requestLine in request.Lines)
        {
            HmoTariffItem? tariffItem = null;
            if (hmoCompanyId is not null && !string.IsNullOrWhiteSpace(requestLine.TariffCode))
                priceBook.TryGetValue(requestLine.TariffCode.Trim(), out tariffItem);
            if (hmoCompanyId is not null && tariffItem is null)
                warnings.Add($"No active HMO tariff matched {requestLine.TariffCode ?? requestLine.Description}. Market price was used.");

            var unitPrice = tariffItem?.UnitPrice ?? requestLine.UnitPrice;
            var line = new BillLine
            {
                Category = requestLine.Category,
                Description = requestLine.Description,
                Quantity = requestLine.Quantity,
                UnitPrice = unitPrice,
                DiscountAmount = 0m,
                TariffItemId = tariffItem?.Id,
                TariffConfirmedAt = tariffItem is null ? null : DateTimeOffset.UtcNow,
                TariffConfirmedBy = tariffItem is null ? null : user.RequireId(),
                SourceType = ChargeSourceType.RegistrationPeriod,
                SourceId = bill.Id,
                SourceKey = $"manual-bill:{bill.Id}:{Guid.NewGuid():N}",
                CreatedBy = user.RequireId()
            };
            BillingCalculator.SetLineAmounts(line, tariffItem?.CoveragePercent ?? 0m, tariffItem is not null);
            bill.Lines.Add(line);
        }

        BillingCalculator.Recalculate(bill);
        db.Bills.Add(bill);
        db.BillStatusHistories.Add(new BillStatusHistory
        {
            BillId = bill.Id,
            FromStatus = null,
            ToStatus = BillStatus.Committed,
            ActorId = user.Id,
            Reason = request.Description ?? "Manual bill created by Finance"
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await audit.WriteAsync("bill.manual-created", nameof(Bill), bill.Id.ToString(), patient.Id, null, bill.Id,
            after: new { bill.BillNumber, bill.PayerType, bill.HmoCompanyId, bill.PatientPayableTotal, bill.HmoPayableTotal },
            cancellationToken: cancellationToken);
        return new ManualBillResultDto(await GetAsync(bill.Id, cancellationToken), warnings);
    }

    public async Task<Bill> CreateAdministrativeBillAsync(
        Guid? patientId, BillKind kind, FeeCode feeCode, Guid sourceId,
        ChargeSourceType sourceType, string description,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var fee = await db.FeeConfigurations
            .Where(x => x.Code == feeCode && x.IsActive && x.EffectiveFrom <= today && (x.EffectiveTo == null || x.EffectiveTo >= today))
            .OrderByDescending(x => x.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new AppConflictException($"No active fee is configured for {feeCode}.");

        var sourceKey = $"{sourceType}:{sourceId}";
        var existing = await db.Bills.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Lines.Any(l => l.SourceKey == sourceKey), cancellationToken);
        if (existing is not null) return existing;

        var bill = new Bill
        {
            BillNumber = await numbers.NextAsync("bill_number_seq", "BILL", cancellationToken),
            PatientId = patientId,
            Kind = kind,
            PayerType = PayerType.SelfPay,
            Status = BillStatus.Committed,
            CommittedAt = DateTimeOffset.UtcNow,
            CommittedBy = user.RequireId(),
            CreatedBy = user.RequireId(),
            Currency = fee.Currency
        };
        ApplyContext(bill, kind, sourceId);

        var line = new BillLine
        {
            BillId = bill.Id,
            Category = MapFeeCategory(feeCode),
            Description = description,
            Quantity = 1m,
            UnitPrice = fee.Amount,
            GrossAmount = fee.Amount,
            NetAmount = fee.Amount,
            HmoAmount = 0m,
            PatientAmount = fee.Amount,
            IsHmoCovered = false,
            SourceType = sourceType,
            SourceId = sourceId,
            SourceKey = sourceKey,
            CreatedBy = user.RequireId()
        };
        bill.Lines.Add(line);
        BillingCalculator.Recalculate(bill);
        db.Bills.Add(bill);
        db.BillStatusHistories.Add(new BillStatusHistory
        {
            BillId = bill.Id,
            FromStatus = null,
            ToStatus = BillStatus.Committed,
            ActorId = user.Id,
            Reason = "Administrative fee auto-committed"
        });
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("bill.admin-created", nameof(Bill), bill.Id.ToString(), patientId, null, bill.Id,
            after: new { bill.BillNumber, bill.Kind, bill.PatientPayableTotal }, cancellationToken: cancellationToken);
        return bill;
    }

    public async Task<Bill> GetOrCreateEncounterBillAsync(Encounter encounter, CancellationToken cancellationToken = default)
    {
        var bill = await db.Bills.Include(x => x.Lines).Include(x => x.Adjustments).Include(x => x.Payments)
            .FirstOrDefaultAsync(x => x.Kind == BillKind.OutpatientEncounter && x.EncounterId == encounter.Id && x.Status != BillStatus.Void, cancellationToken);
        if (bill is not null) return bill;

        PatientHmoEnrollment? enrollment = null;
        if (encounter.HmoEnrollmentId is not null)
            enrollment = await db.PatientHmoEnrollments.Include(x => x.HmoCompany).SingleAsync(x => x.Id == encounter.HmoEnrollmentId, cancellationToken);

        bill = new Bill
        {
            BillNumber = await numbers.NextAsync("bill_number_seq", "BILL", cancellationToken),
            PatientId = encounter.PatientId,
            Kind = BillKind.OutpatientEncounter,
            EncounterId = encounter.Id,
            PayerType = enrollment is null ? PayerType.SelfPay : PayerType.Hmo,
            HmoEnrollmentId = enrollment?.Id,
            HmoCompanyId = enrollment?.HmoCompanyId,
            CreatedBy = user.RequireId(),
            Currency = "NGN"
        };
        db.Bills.Add(bill);
        await db.SaveChangesAsync(cancellationToken);
        return bill;
    }

    public async Task<Bill> GetOrCreateAdmissionBillAsync(Admission admission, CancellationToken cancellationToken = default)
    {
        var bill = await db.Bills.Include(x => x.Lines).Include(x => x.Adjustments).Include(x => x.Payments)
            .FirstOrDefaultAsync(x => x.Kind == BillKind.InpatientAdmission && x.AdmissionId == admission.Id && x.Status != BillStatus.Void, cancellationToken);
        if (bill is not null) return bill;

        PatientHmoEnrollment? enrollment = null;
        if (admission.HmoEnrollmentId is not null)
            enrollment = await db.PatientHmoEnrollments.Include(x => x.HmoCompany).SingleAsync(x => x.Id == admission.HmoEnrollmentId, cancellationToken);

        bill = new Bill
        {
            BillNumber = await numbers.NextAsync("bill_number_seq", "BILL", cancellationToken),
            PatientId = admission.PatientId,
            Kind = BillKind.InpatientAdmission,
            AdmissionId = admission.Id,
            PayerType = enrollment is null ? PayerType.SelfPay : PayerType.Hmo,
            HmoEnrollmentId = enrollment?.Id,
            HmoCompanyId = enrollment?.HmoCompanyId,
            CreatedBy = user.RequireId(),
            Currency = "NGN"
        };
        db.Bills.Add(bill);
        await db.SaveChangesAsync(cancellationToken);
        return bill;
    }

    /// <summary>Idempotently adds a charge and resolves the active HMO tariff when one exists.</summary>
    public async Task<BillLine> AddChargeAsync(
        Guid billId, BillLineCategory category, string description, decimal quantity, decimal unitPrice,
        ChargeSourceType sourceType, Guid sourceId, string sourceKey, Guid? encounterId, Guid? admissionId,
        string? tariffCode, CancellationToken cancellationToken = default)
    {
        var bill = await LoadBillAsync(billId, cancellationToken);
        var existing = bill.Lines.FirstOrDefault(x => x.SourceKey == sourceKey);
        if (existing is not null) return existing;
        if (bill.Status != BillStatus.Draft)
            throw new AppConflictException("Charges can only be added while the bill is in Draft.");

        HmoTariffItem? tariff = null;
        if (bill.PayerType == PayerType.Hmo && bill.HmoCompanyId is not null && !string.IsNullOrWhiteSpace(tariffCode))
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            tariff = await db.HmoTariffItems
                .Include(x => x.HmoTariff)
                .Where(x => x.HmoTariff != null
                    && x.HmoTariff.HmoCompanyId == bill.HmoCompanyId
                    && x.HmoTariff.Status == TariffStatus.Active
                    && x.HmoTariff.EffectiveFrom <= today
                    && (x.HmoTariff.EffectiveTo == null || x.HmoTariff.EffectiveTo >= today)
                    && x.Code == tariffCode
                    && x.IsActive)
                .OrderByDescending(x => x.HmoTariff!.EffectiveFrom)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var actualUnitPrice = tariff?.UnitPrice ?? unitPrice;
        var coverage = tariff?.CoveragePercent ?? 0m;
        var line = new BillLine
        {
            BillId = bill.Id,
            Category = category,
            Description = description,
            Quantity = quantity,
            UnitPrice = actualUnitPrice,
            DiscountAmount = 0m,
            IsHmoCovered = coverage > 0,
            TariffItemId = tariff?.Id,
            SourceType = sourceType,
            SourceId = sourceId,
            SourceKey = sourceKey,
            EncounterId = encounterId,
            AdmissionId = admissionId,
            CreatedBy = user.RequireId(),
            ChargeDate = DateTimeOffset.UtcNow
        };
        BillingCalculator.SetLineAmounts(line, coverage, coverage > 0);
        bill.Lines.Add(line);
        db.BillLines.Add(line);
        BillingCalculator.Recalculate(bill);
        bill.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("bill.line-added", nameof(BillLine), line.Id.ToString(), bill.PatientId, encounterId, bill.Id,
            after: new { line.Category, line.Description, line.NetAmount, line.SourceKey }, cancellationToken: cancellationToken);
        return line;
    }

    public async Task<Bill> CommitAsync(Guid billId, CancellationToken cancellationToken = default)
    {
        var bill = await LoadBillAsync(billId, cancellationToken);
        if (bill.Status != BillStatus.Draft) return bill;
        if (bill.Lines.Count == 0) throw new AppConflictException("A bill cannot be committed without lines.");
        BillingCalculator.Recalculate(bill);
        BillStateMachine.EnsureTransition(bill, BillStatus.Committed);
        Transition(bill, BillStatus.Committed, "Committed to Finance");
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("bill.committed", nameof(Bill), bill.Id.ToString(), bill.PatientId, bill.EncounterId, bill.Id,
            after: new { bill.BillNumber, bill.PatientPayableTotal, bill.HmoPayableTotal }, cancellationToken: cancellationToken);
        return bill;
    }

    public async Task<Bill> RecalculateAsync(Guid billId, CancellationToken cancellationToken = default)
    {
        var bill = await LoadBillAsync(billId, cancellationToken);
        BillingCalculator.Recalculate(bill);
        bill.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return bill;
    }
}
