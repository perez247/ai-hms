using Hms.Domain.Billing;
using Hms.Domain.Entities;
using Hms.Domain.Enums;

namespace Hms.UnitTests;

/// <summary>Payer-aware bill transition and payable invariants.</summary>
public sealed class BillStateMachineTests
{
    [Fact]
    public void Self_pay_committed_bill_can_be_approved()
    {
        var bill = new Bill { Status = BillStatus.Committed, PayerType = PayerType.SelfPay };
        Assert.True(BillStateMachine.CanTransition(bill, BillStatus.Approved));
        BillStateMachine.EnsureTransition(bill, BillStatus.Approved);
    }

    [Fact]
    public void Hmo_committed_bill_cannot_skip_hmo_approval()
    {
        var bill = new Bill { Status = BillStatus.Committed, PayerType = PayerType.Hmo };
        Assert.False(BillStateMachine.CanTransition(bill, BillStatus.Approved));
        Assert.Throws<DomainRuleException>(() => BillStateMachine.EnsureTransition(bill, BillStatus.Approved));
    }

    [Fact]
    public void Hmo_bill_requires_pa_and_both_approvals_before_payable()
    {
        var bill = new Bill
        {
            Status = BillStatus.HmoApproved,
            PayerType = PayerType.Hmo,
            FinanceApprovedAt = DateTimeOffset.UtcNow,
            FinanceApprovedBy = Guid.NewGuid(),
            HmoApprovedAt = DateTimeOffset.UtcNow,
            HmoApprovedBy = Guid.NewGuid()
        };
        Assert.Throws<DomainRuleException>(() => BillStateMachine.EnsurePayable(bill));
        bill.PriorAuthorizationId = Guid.NewGuid();
        BillStateMachine.EnsurePayable(bill);
    }

    [Fact]
    public void Finance_approval_without_hmo_approval_is_not_payable()
    {
        var bill = new Bill
        {
            Status = BillStatus.HmoPending,
            PayerType = PayerType.Hmo,
            FinanceApprovedAt = DateTimeOffset.UtcNow,
            FinanceApprovedBy = Guid.NewGuid(),
            PriorAuthorizationId = Guid.NewGuid()
        };
        Assert.Throws<DomainRuleException>(() => BillStateMachine.EnsurePayable(bill));
    }

    [Fact]
    public void Paid_and_void_are_terminal()
    {
        var paid = new Bill { Status = BillStatus.Paid };
        var voided = new Bill { Status = BillStatus.Void };
        Assert.False(BillStateMachine.CanTransition(paid, BillStatus.Approved));
        Assert.False(BillStateMachine.CanTransition(voided, BillStatus.Draft));
    }
}
