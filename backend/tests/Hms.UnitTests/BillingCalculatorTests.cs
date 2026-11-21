using Hms.Domain.Billing;
using Hms.Domain.Entities;
using Hms.Domain.Enums;

namespace Hms.UnitTests;

/// <summary>Pure billing arithmetic and allocation tests.</summary>
public sealed class BillingCalculatorTests
{
    [Fact]
    public void Allocate_splits_hmo_and_patient_without_losing_kobo()
    {
        var (hmo, patient) = BillingCalculator.Allocate(999.99m, 80m, true);
        Assert.Equal(799.99m, hmo);
        Assert.Equal(200.00m, patient);
        Assert.Equal(999.99m, hmo + patient);
    }

    [Fact]
    public void Uncovered_line_is_fully_patient_payable()
    {
        var (hmo, patient) = BillingCalculator.Allocate(1500m, 100m, false);
        Assert.Equal(0m, hmo);
        Assert.Equal(1500m, patient);
    }

    [Fact]
    public void Line_amounts_are_consistent()
    {
        var line = new BillLine { Quantity = 3m, UnitPrice = 333.335m, DiscountAmount = 0.50m };
        BillingCalculator.SetLineAmounts(line, 75m, true);
        Assert.Equal(1000.01m, line.GrossAmount);
        Assert.Equal(999.51m, line.NetAmount);
        Assert.Equal(749.63m, line.HmoAmount);
        Assert.Equal(249.88m, line.PatientAmount);
        Assert.Equal(line.NetAmount, line.HmoAmount + line.PatientAmount);
    }

    [Fact]
    public void Bill_totals_apply_approved_adjustments_and_payments()
    {
        var bill = new Bill { PayerType = PayerType.Hmo };
        bill.Lines.Add(Line(1000m, 800m, 200m));
        bill.Lines.Add(Line(500m, 500m, 0m));
        bill.Adjustments.Add(new BillAdjustment { Amount = 50m, Status = AdjustmentStatus.Approved });
        bill.Adjustments.Add(new BillAdjustment { Amount = 500m, Status = AdjustmentStatus.Proposed });
        bill.Payments.Add(new Payment { Amount = 100m });
        BillingCalculator.Recalculate(bill);
        Assert.Equal(1500m, bill.Subtotal);
        Assert.Equal(50m, bill.DiscountTotal);
        Assert.Equal(1300m, bill.HmoPayableTotal);
        Assert.Equal(150m, bill.PatientPayableTotal);
        Assert.Equal(100m, bill.PaidTotal);
        Assert.Equal(1350m, BillingCalculator.Outstanding(bill));
    }

    [Fact]
    public void Bed_nights_are_per_night_with_minimum_one()
    {
        var admitted = new DateOnly(2026, 11, 20);
        Assert.Equal(1, BillingCalculator.BedNights(admitted, admitted));
        Assert.Equal(2, BillingCalculator.BedNights(admitted, admitted.AddDays(2)));
        Assert.Equal(3, BillingCalculator.BedNights(admitted, admitted.AddDays(2), chargeDischargeDay: true));
    }

    [Fact]
    public void Invalid_line_split_is_rejected()
    {
        var bill = new Bill();
        bill.Lines.Add(Line(100m, 90m, 5m));
        Assert.Throws<DomainRuleException>(() => BillingCalculator.Recalculate(bill));
    }

    private static BillLine Line(decimal net, decimal hmo, decimal patient) => new()
    {
        GrossAmount = net,
        NetAmount = net,
        HmoAmount = hmo,
        PatientAmount = patient,
        Quantity = 1m,
        UnitPrice = net
    };
}
