using Hms.Application.Common;
using Hms.Infrastructure.Persistence;
using Hms.Application.DTOs;
using Hms.Application.Services;
using Hms.Domain.Billing;
using Hms.Domain.Entities;
using Hms.Domain.Enums;
using Microsoft.EntityFrameworkCore;
namespace Hms.Infrastructure.Services;

/// <summary>Read-side bill projections and paging.</summary>
public sealed partial class BillingService
{

    public async Task<PagedResult<BillDto>> SearchAsync(BillStatus? status, Guid? patientId, int page, int pageSize, CancellationToken cancellationToken = default, bool? inDebt = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.Bills.AsNoTracking().AsQueryable();
        if (status is not null) query = query.Where(x => x.Status == status);
        if (patientId is not null) query = query.Where(x => x.PatientId == patientId);
        // The Bills page hides active debts in its main view; the Debt view asks for them explicitly.
        if (inDebt == true) query = query.Where(x => x.IsDebt && x.DebtSettledAt == null);
        else if (inDebt == false) query = query.Where(x => !x.IsDebt || x.DebtSettledAt != null);
        var total = await query.CountAsync(cancellationToken);
        var bills = await query
            .Include(x => x.Patient).Include(x => x.FitnessCertificate).Include(x => x.HmoCompany).Include(x => x.PriorAuthorization)
            .Include(x => x.Lines).Include(x => x.Payments).Include(x => x.Adjustments).Include(x => x.Receivable)
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<BillDto>(bills.Select(MapSearch).ToList(), page, pageSize, total);
    }

    private static BillDto MapSearch(Bill bill) => new(
        bill.Id, bill.BillNumber, bill.Kind, bill.Status, bill.PatientId, bill.Patient?.PatientNumber, bill.Patient?.FullName ?? "Fitness client",
        bill.PayerType, bill.HmoCompanyId, bill.HmoCompany?.Name, bill.PriorAuthorizationId,
        bill.PriorAuthorization?.PaCode, bill.Currency, bill.Subtotal, bill.DiscountTotal,
        bill.HmoPayableTotal, bill.PatientPayableTotal, bill.PaidTotal,
        Math.Max(0m, bill.PatientPayableTotal + bill.HmoPayableTotal - bill.PaidTotal),
        bill.EncounterId, bill.AdmissionId, bill.AppointmentId, bill.RegistrationPeriodId, bill.FitnessCertificateId,
        bill.CreatedAt, bill.CommittedAt, bill.HmoApprovedAt, bill.FinanceApprovedAt, bill.PaidAt,
        bill.Lines.OrderBy(x => x.CreatedAt).Select(x => new BillLineDto(
            x.Id, x.Category, ItemTypeFor(x), x.Description, x.Quantity, x.UnitPrice, x.GrossAmount, x.DiscountAmount,
            x.NetAmount, x.HmoAmount, x.PatientAmount, x.IsHmoCovered, x.TariffItemId,
            x.TariffConfirmedAt, x.ReferralDestination, x.EncounterId, x.AdmissionId, x.ChargeDate, x.CreatedBy)).ToList(),
        bill.Payments.Select(x => new PaymentDto(x.Id, x.Amount, x.Method, x.Reference, x.ReceivedAt, x.RecordedBy)).ToList(),
        bill.Adjustments.Select(x => new BillAdjustmentDto(x.Id, x.Amount, x.Reason, x.Status, x.CreatedAt, x.ApprovedBy)).ToList(),
        null,
        bill.Receivable is null ? null : new HmoReceivableDto(bill.Receivable.Id, bill.Receivable.PaCode, bill.Receivable.Amount, bill.Receivable.SettledAmount, bill.Receivable.Status, bill.Receivable.DueDate),
        bill.IsDebt, bill.DebtOwner, bill.DebtRecordedAt, bill.DebtReason, bill.DebtSettledAt);

    public async Task<BillDto> GetAsync(Guid billId, CancellationToken cancellationToken = default)
    {
        var bill = await db.Bills.AsNoTracking()
            .Include(x => x.Patient).Include(x => x.FitnessCertificate).Include(x => x.HmoCompany).Include(x => x.PriorAuthorization)
            .Include(x => x.Lines).Include(x => x.Payments).Include(x => x.Adjustments).Include(x => x.Receivable)
            .SingleOrDefaultAsync(x => x.Id == billId, cancellationToken)
            ?? throw new AppNotFoundException("Bill not found.");
        return await Map(bill, cancellationToken);
    }

    private async Task<BillDto> Map(Bill bill, CancellationToken cancellationToken)
    {
        var medicationAdministrations = new List<MedicationAdministrationDto>();
        if (bill.AdmissionId is not null)
        {
            var administrations = await db.MedicationAdministrations
                .Include(x => x.Encounter)
                .Include(x => x.PrescriptionItem).ThenInclude(x => x.StockItem)
                .Where(x => x.Encounter.AdmissionId == bill.AdmissionId)
                .OrderByDescending(x => x.AdministeredAt)
                .ToListAsync(cancellationToken);
            foreach (var admin in administrations)
            {
                var charged = admin.DispenseItemId != Guid.Empty &&
                    bill.Lines.Any(l => l.SourceKey == $"med-admin:{admin.Id}");
                medicationAdministrations.Add(new MedicationAdministrationDto(
                    admin.Id,
                    admin.PrescriptionItem?.Medication?.Name ?? "Medication",
                    admin.PrescriptionItem?.Dose,
                    admin.PrescriptionItem?.Route,
                    admin.DoseGiven,
                    admin.AdministeredAt,
                    admin.NurseName,
                    admin.Accepted,
                    admin.NonAcceptanceReason,
                    charged));
            }
        }

        return new BillDto(
            bill.Id, bill.BillNumber, bill.Kind, bill.Status, bill.PatientId, bill.Patient?.PatientNumber, bill.Patient?.FullName ?? "Fitness client",
            bill.PayerType, bill.HmoCompanyId, bill.HmoCompany?.Name, bill.PriorAuthorizationId,
            bill.PriorAuthorization?.PaCode, bill.Currency, bill.Subtotal, bill.DiscountTotal,
            bill.HmoPayableTotal, bill.PatientPayableTotal, bill.PaidTotal,
            Math.Max(0m, bill.PatientPayableTotal + bill.HmoPayableTotal - bill.PaidTotal),
            bill.EncounterId, bill.AdmissionId, bill.AppointmentId, bill.RegistrationPeriodId, bill.FitnessCertificateId,
            bill.CreatedAt, bill.CommittedAt, bill.HmoApprovedAt, bill.FinanceApprovedAt, bill.PaidAt,
            bill.Lines.OrderBy(x => x.CreatedAt).Select(x => new BillLineDto(
                x.Id, x.Category, ItemTypeFor(x), x.Description, x.Quantity, x.UnitPrice, x.GrossAmount, x.DiscountAmount,
                x.NetAmount, x.HmoAmount, x.PatientAmount, x.IsHmoCovered, x.TariffItemId,
                x.TariffConfirmedAt, x.ReferralDestination, x.EncounterId, x.AdmissionId, x.ChargeDate, x.CreatedBy)).ToList(),
            bill.Payments.Select(x => new PaymentDto(x.Id, x.Amount, x.Method, x.Reference, x.ReceivedAt, x.RecordedBy)).ToList(),
            bill.Adjustments.Select(x => new BillAdjustmentDto(x.Id, x.Amount, x.Reason, x.Status, x.CreatedAt, x.ApprovedBy)).ToList(),
            medicationAdministrations,
            bill.Receivable is null ? null : new HmoReceivableDto(bill.Receivable.Id, bill.Receivable.PaCode, bill.Receivable.Amount, bill.Receivable.SettledAmount, bill.Receivable.Status, bill.Receivable.DueDate),
            bill.IsDebt, bill.DebtOwner, bill.DebtRecordedAt, bill.DebtReason, bill.DebtSettledAt);
    }

    /// <summary>
    /// Classifies a bill line as a catalogue item so the compact invoice can group it by item type.
    /// Finance-added lines use <see cref="ChargeSourceType.Encounter"/> and inherit the category type.
    /// </summary>
    private static StockItemType? ItemTypeFor(BillLine line) => line.SourceType switch
    {
        ChargeSourceType.MedicationDispenseItem or ChargeSourceType.MedicationAdministration => StockItemType.Medication,
        ChargeSourceType.InvestigationOrder => StockItemType.Test,
        ChargeSourceType.SurgeryCase => StockItemType.Procedure,
        ChargeSourceType.Encounter => line.Category switch
        {
            BillLineCategory.Medication => StockItemType.Medication,
            BillLineCategory.Investigation => StockItemType.Test,
            BillLineCategory.Surgery => StockItemType.Procedure,
            BillLineCategory.Consultation or BillLineCategory.Emergency => StockItemType.Service,
            _ => null
        },
        _ => null
    };
}
