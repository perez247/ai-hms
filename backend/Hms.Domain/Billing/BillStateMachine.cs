using Hms.Domain.Entities;
using Hms.Domain.Enums;

namespace Hms.Domain.Billing;

/// <summary>
/// Sole authority for bill status transitions. HMO bills must pass HMO approval before
/// Finance can mark them payable; self-pay bills go directly from Committed to Approved.
/// </summary>
public static class BillStateMachine
{
    private static readonly IReadOnlyDictionary<BillStatus, BillStatus[]> Allowed =
        new Dictionary<BillStatus, BillStatus[]>
        {
            [BillStatus.Draft] = [BillStatus.Committed, BillStatus.Void],
            [BillStatus.Committed] = [BillStatus.Approved, BillStatus.HmoPending, BillStatus.Void],
            [BillStatus.HmoPending] = [BillStatus.HmoApproved, BillStatus.Void],
            [BillStatus.HmoApproved] = [BillStatus.Approved, BillStatus.Void],
            [BillStatus.Approved] = [BillStatus.Paid, BillStatus.Void],
            [BillStatus.Paid] = [],
            [BillStatus.Void] = []
        };

    /// <summary>Returns false for unknown transitions and for payer-specific illegal transitions.</summary>
    public static bool CanTransition(Bill bill, BillStatus next)
    {
        if (!Allowed.TryGetValue(bill.Status, out var targets) || !targets.Contains(next))
            return false;

        if (next == BillStatus.HmoPending && bill.PayerType != PayerType.Hmo) return false;
        if (bill.Status == BillStatus.Committed && next == BillStatus.Approved && bill.PayerType == PayerType.Hmo)
            return false;
        if (bill.Status == BillStatus.HmoApproved && next == BillStatus.Approved && bill.PayerType != PayerType.Hmo)
            return false;
        if (next == BillStatus.HmoApproved && bill.PayerType != PayerType.Hmo)
            return false;

        return true;
    }

    public static void EnsureTransition(Bill bill, BillStatus next)
    {
        if (!CanTransition(bill, next))
            throw new DomainRuleException($"Bill cannot transition from {bill.Status} to {next} for a {bill.PayerType} bill.");
    }

    /// <summary>Throws unless Finance (and HMO, when applicable) approval plus a PA are present.</summary>
    public static void EnsurePayable(Bill bill)
    {
        if (bill.FinanceApprovedAt is null || bill.FinanceApprovedBy is null)
            throw new DomainRuleException("Finance approval is required before a bill is payable.");

        if (bill.PayerType == PayerType.Hmo)
        {
            if (bill.HmoApprovedAt is null || bill.HmoApprovedBy is null)
                throw new DomainRuleException("HMO approval is required before an HMO bill is payable.");
            if (bill.PriorAuthorizationId is null)
                throw new DomainRuleException("A prior authorization is required before an HMO bill is payable.");
        }
    }
}
