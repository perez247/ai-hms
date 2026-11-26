using Hms.Application.Common;
using Hms.Infrastructure.Persistence;
using Hms.Application.DTOs;
using Hms.Application.Services;
using Hms.Domain.Billing;
using Hms.Domain.Entities;
using Hms.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Hms.Infrastructure.Services;

/// <summary>Shared bill loading, status-history and context/fee mapping helpers.</summary>
public sealed partial class BillingService
{

    private async Task<Bill> LoadBillAsync(Guid billId, CancellationToken cancellationToken) =>
        await db.Bills
            .Include(x => x.Patient)
            .Include(x => x.FitnessCertificate)
            .Include(x => x.HmoCompany)
            .Include(x => x.PriorAuthorization)
            .Include(x => x.Lines)
            .Include(x => x.Adjustments)
            .Include(x => x.Payments)
            .Include(x => x.Receivable)
            .SingleOrDefaultAsync(x => x.Id == billId, cancellationToken)
        ?? throw new AppNotFoundException("Bill not found.");

    private void Transition(Bill bill, BillStatus next, string reason)
    {
        var from = bill.Status;
        BillStateMachine.EnsureTransition(bill, next);
        bill.Status = next;
        if (next == BillStatus.Committed) bill.CommittedAt ??= DateTimeOffset.UtcNow;
        if (next == BillStatus.Paid) bill.PaidAt ??= DateTimeOffset.UtcNow;
        bill.UpdatedAt = DateTimeOffset.UtcNow;
        db.BillStatusHistories.Add(new BillStatusHistory
        {
            BillId = bill.Id,
            FromStatus = from,
            ToStatus = next,
            ActorId = user.Id,
            Reason = reason
        });
    }

    private static void SettleDebtIfCleared(Bill bill)
    {
        if (bill.IsDebt && bill.DebtSettledAt is null && BillingCalculator.Outstanding(bill) == 0m)
            bill.DebtSettledAt = DateTimeOffset.UtcNow;
    }

    private static void ApplyContext(Bill bill, BillKind kind, Guid sourceId)
    {
        switch (kind)
        {
            case BillKind.Registration: bill.RegistrationPeriodId = sourceId; break;
            case BillKind.Appointment: bill.AppointmentId = sourceId; break;
            case BillKind.Emergency: bill.EncounterId = sourceId; break;
            case BillKind.FitnessCertificate: bill.FitnessCertificateId = sourceId; break;
            default: throw new AppConflictException("Unsupported administrative bill kind.");
        }
    }

    private static BillLineCategory MapFeeCategory(FeeCode code) => code switch
    {
        FeeCode.RegistrationNew => BillLineCategory.Registration,
        FeeCode.RegistrationRenewal => BillLineCategory.RegistrationRenewal,
        FeeCode.Appointment => BillLineCategory.Appointment,
        FeeCode.Emergency => BillLineCategory.Emergency,
        FeeCode.InpatientConsultation => BillLineCategory.Consultation,
        FeeCode.BedDayGeneral or FeeCode.BedDayPrivate => BillLineCategory.BedDay,
        FeeCode.FitnessCertificate => BillLineCategory.FitnessCertificate,
        _ => BillLineCategory.FitnessCertificate
    };
}
