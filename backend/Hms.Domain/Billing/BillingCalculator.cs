using Hms.Domain.Entities;
using Hms.Domain.Enums;

namespace Hms.Domain.Billing;

/// <summary>
/// Pure money rules shared by all billing scenarios. Callers must supply fully populated
/// lines and adjustments; no database or clock access happens here.
/// </summary>
public static class BillingCalculator
{
    /// <summary>Splits a net line into HMO and patient portions, rounding each line to two decimals.</summary>
    public static (decimal HmoAmount, decimal PatientAmount) Allocate(decimal netAmount, decimal coveragePercent, bool isCovered)
    {
        if (netAmount < 0) throw new DomainRuleException("Net amount cannot be negative.");
        if (!isCovered) return (0m, netAmount);
        var percent = Math.Clamp(coveragePercent, 0m, 100m);
        var hmo = Math.Round(netAmount * percent / 100m, 2, MidpointRounding.AwayFromZero);
        return (hmo, netAmount - hmo);
    }

    /// <summary>Calculates and validates one line's gross/net/HMO/patient amounts in place.</summary>
    public static void SetLineAmounts(BillLine line, decimal coveragePercent, bool isCovered)
    {
        line.GrossAmount = Math.Round(line.Quantity * line.UnitPrice, 2, MidpointRounding.AwayFromZero);
        line.NetAmount = Math.Round(line.GrossAmount - line.DiscountAmount, 2, MidpointRounding.AwayFromZero);
        if (line.NetAmount < 0) throw new DomainRuleException("Line discount cannot exceed the gross amount.");
        var (hmo, patient) = Allocate(line.NetAmount, coveragePercent, isCovered);
        line.HmoAmount = hmo;
        line.PatientAmount = patient;
        line.IsHmoCovered = isCovered;
    }

    /// <summary>Recomputes all bill totals from lines, approved adjustments and payments.</summary>
    public static void Recalculate(Bill bill)
    {
        foreach (var line in bill.Lines)
        {
            if (line.NetAmount < 0 || line.HmoAmount < 0 || line.PatientAmount < 0)
                throw new DomainRuleException("Bill line amounts cannot be negative.");
            if (Math.Round(line.HmoAmount + line.PatientAmount, 2) != Math.Round(line.NetAmount, 2))
                throw new DomainRuleException("Line HMO + patient amount must equal the net amount.");
        }

        bill.Subtotal = bill.Lines.Sum(x => x.NetAmount);
        bill.DiscountTotal = bill.Adjustments
            .Where(x => x.Status == AdjustmentStatus.Approved)
            .Sum(x => x.Amount);
        bill.HmoPayableTotal = bill.Lines.Sum(x => x.HmoAmount);
        bill.PatientPayableTotal = Math.Max(0m, bill.Lines.Sum(x => x.PatientAmount) - bill.DiscountTotal);
        bill.PaidTotal = bill.Payments.Sum(x => x.Amount);
    }

    /// <summary>Amount still owed by both payer portions after recorded payments.</summary>
    public static decimal Outstanding(Bill bill) =>
        Math.Max(0m, bill.PatientPayableTotal + bill.HmoPayableTotal - bill.PaidTotal);

    /// <summary>Counts billable overnights; a same-day admission/discharge still bills one night.</summary>
    public static int BedNights(DateOnly admittedOn, DateOnly dischargedOn, bool chargeDischargeDay = false)
    {
        if (dischargedOn < admittedOn) throw new DomainRuleException("Discharge date cannot precede admission date.");
        var nights = dischargedOn.DayNumber - admittedOn.DayNumber;
        if (chargeDischargeDay) nights++;
        return Math.Max(1, nights);
    }
}
