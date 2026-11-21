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

    public async Task<PagedResult<BillDto>> SearchAsync(BillStatus? status, Guid? patientId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.Bills.AsNoTracking().AsQueryable();
        if (status is not null) query = query.Where(x => x.Status == status);
        if (patientId is not null) query = query.Where(x => x.PatientId == patientId);
        var total = await query.CountAsync(cancellationToken);
        var bills = await query
            .Include(x => x.Patient).Include(x => x.FitnessCertificate).Include(x => x.HmoCompany).Include(x => x.PriorAuthorization)
            .Include(x => x.Lines).Include(x => x.Payments).Include(x => x.Adjustments).Include(x => x.Receivable)
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<BillDto>(bills.Select(Map).ToList(), page, pageSize, total);
    }

    public async Task<BillDto> GetAsync(Guid billId, CancellationToken cancellationToken = default)
    {
        var bill = await db.Bills.AsNoTracking()
            .Include(x => x.Patient).Include(x => x.FitnessCertificate).Include(x => x.HmoCompany).Include(x => x.PriorAuthorization)
            .Include(x => x.Lines).Include(x => x.Payments).Include(x => x.Adjustments).Include(x => x.Receivable)
            .SingleOrDefaultAsync(x => x.Id == billId, cancellationToken)
            ?? throw new AppNotFoundException("Bill not found.");
        return Map(bill);
    }

    private static BillDto Map(Bill bill) => new(
        bill.Id, bill.BillNumber, bill.Kind, bill.Status, bill.PatientId, bill.Patient?.PatientNumber, bill.Patient?.FullName ?? "Fitness client",
        bill.PayerType, bill.HmoCompanyId, bill.HmoCompany?.Name, bill.PriorAuthorizationId,
        bill.PriorAuthorization?.PaCode, bill.Currency, bill.Subtotal, bill.DiscountTotal,
        bill.HmoPayableTotal, bill.PatientPayableTotal, bill.PaidTotal,
        Math.Max(0m, bill.PatientPayableTotal + bill.HmoPayableTotal - bill.PaidTotal),
        bill.EncounterId, bill.AdmissionId, bill.AppointmentId, bill.RegistrationPeriodId, bill.FitnessCertificateId,
        bill.CreatedAt, bill.CommittedAt, bill.HmoApprovedAt, bill.FinanceApprovedAt, bill.PaidAt,
        bill.Lines.OrderBy(x => x.CreatedAt).Select(x => new BillLineDto(
            x.Id, x.Category, x.Description, x.Quantity, x.UnitPrice, x.GrossAmount, x.DiscountAmount,
            x.NetAmount, x.HmoAmount, x.PatientAmount, x.IsHmoCovered, x.TariffItemId,
            x.TariffConfirmedAt, x.ReferralDestination, x.EncounterId, x.AdmissionId, x.ChargeDate, x.CreatedBy)).ToList(),
        bill.Payments.Select(x => new PaymentDto(x.Id, x.Amount, x.Method, x.Reference, x.ReceivedAt, x.RecordedBy)).ToList(),
        bill.Adjustments.Select(x => new BillAdjustmentDto(x.Id, x.Amount, x.Reason, x.Status, x.CreatedAt, x.ApprovedBy)).ToList(),
        bill.Receivable is null ? null : new HmoReceivableDto(bill.Receivable.Id, bill.Receivable.PaCode, bill.Receivable.Amount, bill.Receivable.SettledAmount, bill.Receivable.Status, bill.Receivable.DueDate));
}
