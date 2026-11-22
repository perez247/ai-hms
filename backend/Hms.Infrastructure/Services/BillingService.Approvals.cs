using Hms.Application.Common;
using Hms.Infrastructure.Persistence;
using Hms.Application.DTOs;
using Hms.Application.Services;
using Hms.Domain.Billing;
using Hms.Domain.Entities;
using Hms.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Hms.Infrastructure.Services;

/// <summary>Payer-aware approval, negotiation and settlement transitions for bills.</summary>
public sealed partial class BillingService
{

    public async Task<Bill> RouteToHmoAsync(Guid billId, CancellationToken cancellationToken = default)
    {
        var bill = await LoadBillAsync(billId, cancellationToken);
        BillStateMachine.EnsureTransition(bill, BillStatus.HmoPending);
        if (bill.PayerType != PayerType.Hmo) throw new AppConflictException("Only HMO bills can be routed to the HMO Office.");
        bill.HmoSubmittedAt = DateTimeOffset.UtcNow;
        Transition(bill, BillStatus.HmoPending, "Routed to HMO Office");
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("bill.routed-hmo", nameof(Bill), bill.Id.ToString(), bill.PatientId, bill.EncounterId, bill.Id, cancellationToken: cancellationToken);
        return bill;
    }

    /// <summary>Finance approval gate. HMO bills need HMO approval, a PA and co-pay before becoming payable.</summary>
    public async Task<Bill> ApproveAsync(Guid billId, string? notes, CancellationToken cancellationToken = default)
    {
        var bill = await LoadBillAsync(billId, cancellationToken);
        if (bill.Status is BillStatus.Paid or BillStatus.Void) return bill;

        if (bill.PayerType == PayerType.SelfPay)
        {
            if (bill.Status != BillStatus.Committed)
                throw new AppConflictException("Only a committed self-pay bill can be approved.");
            if (bill.PaidTotal < bill.PatientPayableTotal)
                throw new AppConflictException("Record the patient payment before Finance approval.");
        }
        else
        {
            if (bill.Status != BillStatus.HmoApproved)
                throw new AppConflictException("The HMO Office must approve the bill before Finance approval.");
            if (bill.PriorAuthorization is null || string.IsNullOrWhiteSpace(bill.PriorAuthorization.PaCode))
                throw new AppConflictException("A PA code is required before Finance approval.");
            if (bill.PaidTotal < bill.PatientPayableTotal)
                throw new AppConflictException("Record the patient co-pay before Finance approval.");
        }

        bill.FinanceApprovedAt = DateTimeOffset.UtcNow;
        bill.FinanceApprovedBy = user.RequireId();
        BillStateMachine.EnsureTransition(bill, BillStatus.Approved);
        Transition(bill, BillStatus.Approved, notes ?? "Finance approved");

        if (bill.PayerType == PayerType.Hmo && bill.Receivable is null)
        {
            var pa = bill.PriorAuthorization!;
            db.HmoReceivables.Add(new HmoReceivable
            {
                BillId = bill.Id,
                HmoCompanyId = bill.HmoCompanyId!.Value,
                PriorAuthorizationId = pa.Id,
                PaCode = pa.PaCode,
                Amount = bill.HmoPayableTotal,
                RecordedBy = user.RequireId(),
                DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30))
            });
        }

        if (BillingCalculator.Outstanding(bill) == 0m)
        {
            BillStateMachine.EnsurePayable(bill);
            Transition(bill, BillStatus.Paid, "Settled at approval");
        }

        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("bill.finance-approved", nameof(Bill), bill.Id.ToString(), bill.PatientId, bill.EncounterId, bill.Id,
            after: new { bill.Status, bill.FinanceApprovedAt, bill.HmoPayableTotal, bill.PatientPayableTotal }, cancellationToken: cancellationToken);
        return bill;
    }

    public async Task<Bill> NegotiateAsync(Guid billId, NegotiateBillRequest request, CancellationToken cancellationToken = default)
    {
        if (request.DiscountAmount <= 0) throw new AppConflictException("Discount amount must be greater than zero.");
        var bill = await LoadBillAsync(billId, cancellationToken);
        if (bill.Status is BillStatus.Approved or BillStatus.Paid or BillStatus.Void)
            throw new AppConflictException("Price negotiation is not allowed after Finance approval.");
        if (request.DiscountAmount > bill.Lines.Sum(x => x.PatientAmount))
            throw new AppConflictException("Discount cannot exceed the patient portion.");

        var adjustment = new BillAdjustment
        {
            BillId = bill.Id,
            Amount = request.DiscountAmount,
            Reason = request.Reason,
            Status = AdjustmentStatus.Approved,
            RequestedBy = user.RequireId(),
            ApprovedBy = user.RequireId(),
            ApprovedAt = DateTimeOffset.UtcNow
        };
        bill.Adjustments.Add(adjustment);
        db.BillAdjustments.Add(adjustment);
        BillingCalculator.Recalculate(bill);
        bill.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("bill.negotiated", nameof(Bill), bill.Id.ToString(), bill.PatientId, bill.EncounterId, bill.Id,
            after: new { request.DiscountAmount, request.Reason, bill.PatientPayableTotal }, cancellationToken: cancellationToken);
        return bill;
    }

    /// <summary>Posts payment/income and moves an approved fully-paid bill to Paid.</summary>
    public async Task<Bill> RecordPaymentAsync(Guid billId, PaymentRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0) throw new AppConflictException("Payment amount must be greater than zero.");
        var bill = await LoadBillAsync(billId, cancellationToken);
        if (bill.Status is BillStatus.Draft or BillStatus.Void)
            throw new AppConflictException("Payments cannot be recorded against a draft or void bill.");
        if (BillingCalculator.Outstanding(bill) < request.Amount)
            throw new AppConflictException("Payment exceeds the outstanding balance.");
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey) &&
            await db.Payments.AnyAsync(x => x.IdempotencyKey == request.IdempotencyKey, cancellationToken))
            return bill;

        var payment = new Payment
        {
            BillId = bill.Id,
            Amount = request.Amount,
            Method = request.Method,
            Reference = request.Reference,
            IdempotencyKey = request.IdempotencyKey,
            Notes = request.Notes,
            RecordedBy = user.RequireId()
        };
        bill.Payments.Add(payment);
        db.Payments.Add(payment);
        BillingCalculator.Recalculate(bill);

        if (request.Method == PaymentMethod.HmoRemittance)
        {
            var receivable = bill.Receivable ?? throw new AppConflictException("No HMO receivable exists for this bill.");
            receivable.SettledAmount += request.Amount;
            receivable.Status = receivable.SettledAmount >= receivable.Amount ? HmoReceivableStatus.Settled : HmoReceivableStatus.PartiallySettled;
            if (receivable.Status == HmoReceivableStatus.Settled) receivable.SettledAt = DateTimeOffset.UtcNow;
        }

        db.AccountBookEntries.Add(new AccountBookEntry
        {
            EntryNumber = await numbers.NextAsync("account_entry_seq", "ACC", cancellationToken),
            EntryType = AccountEntryType.Income,
            Category = request.Method == PaymentMethod.HmoRemittance ? "HMO remittance" : "Patient payment",
            Amount = request.Amount,
            Currency = bill.Currency,
            Description = $"Payment for {bill.BillNumber}",
            Reference = request.Reference,
            BillId = bill.Id,
            PaymentId = payment.Id,
            CreatedBy = user.RequireId()
        });

        if (bill.Status == BillStatus.Approved && BillingCalculator.Outstanding(bill) == 0m)
        {
            BillStateMachine.EnsurePayable(bill);
            Transition(bill, BillStatus.Paid, "Final payment received");
            bill.PaidAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("bill.payment-recorded", nameof(Payment), payment.Id.ToString(), bill.PatientId, bill.EncounterId, bill.Id,
            after: new { payment.Amount, payment.Method, bill.PaidTotal, bill.Status }, cancellationToken: cancellationToken);
        return bill;
    }
}
