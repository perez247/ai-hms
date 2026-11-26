using System.Net;
using System.Net.Http.Json;
using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Domain.Enums;

namespace Hms.IntegrationTests;

/// <summary>Role boundaries for Bills/Account book, reason-based appointment pricing and external credits.</summary>
public sealed class AccessInvoiceAndCreditTests(HmsApiFactory factory) : IClassFixture<HmsApiFactory>
{
    [Fact]
    public async Task Pharmacy_works_the_bills_page_but_cannot_open_or_post_to_the_account_book()
    {
        var reception = await TestData.ClientAsync(factory, "receptionist@clinic.local");
        var pharmacy = await TestData.ClientAsync(factory, "pharmacy@clinic.local");

        var bills = await pharmacy.GetAsync("/api/billing/bills?pageSize=5");
        Assert.Equal(HttpStatusCode.OK, bills.StatusCode);

        // Pharmacy can now create, negotiate, settle and approve a bill from the Bills page.
        var patient = await TestData.CreatePatientAsync(reception);
        var created = await pharmacy.PostAsJsonAsync("/api/billing/bills", new CreateManualBillRequest(
            patient.Id, null, null, "Pharmacy bill page test",
            [new CreateManualBillLineRequest(BillLineCategory.Medication, "Paracetamol", 2m, 500m, null)]), TestData.Json);
        created.EnsureSuccessStatusCode();
        var bill = (await created.Content.ReadFromJsonAsync<ManualBillResultDto>(TestData.Json))!.Bill;
        Assert.Equal(BillStatus.Committed, bill.Status);

        var negotiated = await pharmacy.PostAsJsonAsync($"/api/billing/bills/{bill.Id}/negotiate",
            new NegotiateBillRequest(100m, "Pharmacy negotiated discount"), TestData.Json);
        negotiated.EnsureSuccessStatusCode();
        var afterDiscount = await negotiated.Content.ReadFromJsonAsync<BillDto>(TestData.Json);
        Assert.NotNull(afterDiscount);
        Assert.Equal(900m, afterDiscount.PatientPayableTotal);

        var payment = await pharmacy.PostAsJsonAsync($"/api/billing/bills/{bill.Id}/payments",
            new PaymentRequest(afterDiscount.PatientPayableTotal, PaymentMethod.Cash, "PHARMACY-CASH", Guid.NewGuid().ToString(), null), TestData.Json);
        payment.EnsureSuccessStatusCode();

        var approval = await pharmacy.PostAsJsonAsync($"/api/billing/bills/{bill.Id}/approve",
            new ApproveBillRequest("Pharmacy approval"), TestData.Json);
        approval.EnsureSuccessStatusCode();
        var approved = await approval.Content.ReadFromJsonAsync<BillDto>(TestData.Json);
        Assert.NotNull(approved);
        Assert.Equal(BillStatus.Paid, approved.Status);

        // The account book stays Finance-only even though Bills is shared.
        var accountBook = await pharmacy.GetAsync("/api/billing/account-book");
        Assert.Equal(HttpStatusCode.Forbidden, accountBook.StatusCode);

        var credit = await pharmacy.PostAsJsonAsync("/api/billing/account-book/credits",
            new RecordCreditRequest(1_000m, "Grant", DateTimeOffset.UtcNow, "Should be forbidden", null), TestData.Json);
        Assert.Equal(HttpStatusCode.Forbidden, credit.StatusCode);
    }

    [Fact]
    public async Task Appointment_quote_changes_with_reason_and_payer_and_is_used_on_the_visit_bill()
    {
        var reception = await TestData.ClientAsync(factory, "receptionist@clinic.local");
        var doctor = await TestData.ClientAsync(factory, "doctor@clinic.local");
        var doctorId = await TestData.CurrentUserIdAsync(doctor);

        var consultation = await reception.GetFromJsonAsync<AppointmentPriceQuoteDto>(
            "/api/appointments/quote?reason=Consultation&fundingSource=Private", TestData.Json);
        Assert.NotNull(consultation);
        Assert.Equal(5_000m, consultation!.UnitPrice);
        Assert.Equal(5_000m, consultation.PatientAmount);
        Assert.Equal(0m, consultation.HmoAmount);

        var emergency = await reception.GetFromJsonAsync<AppointmentPriceQuoteDto>(
            "/api/appointments/quote?reason=Emergency&fundingSource=Private", TestData.Json);
        Assert.NotNull(emergency);
        Assert.Equal(20_000m, emergency!.UnitPrice);

        var hmoId = await TestData.FirstHmoIdAsync(reception);
        var hmoPatient = await TestData.CreatePatientAsync(reception, hmoId);
        var enrollments = await doctor.GetFromJsonAsync<List<HmoEnrollmentOptionDto>>(
            $"/api/reference/patients/{hmoPatient.Id}/hmo-enrollments", TestData.Json);
        var enrollment = Assert.Single(enrollments!);

        var hmoQuote = await reception.GetFromJsonAsync<AppointmentPriceQuoteDto>(
            $"/api/appointments/quote?reason=Consultation&fundingSource=Hmo&hmoEnrollmentId={enrollment.Id}", TestData.Json);
        Assert.NotNull(hmoQuote);
        Assert.Equal(4_500m, hmoQuote!.UnitPrice);
        Assert.Equal(90, hmoQuote.CoveragePercent);
        Assert.Equal(4_050m, hmoQuote.HmoAmount);
        Assert.Equal(450m, hmoQuote.PatientAmount);

        // Invalid free-text reasons are rejected by the validator.
        var invalid = await reception.PostAsJsonAsync("/api/appointments", new CreateAppointmentRequest(
            hmoPatient.PatientNumber, doctorId, DateTimeOffset.UtcNow.AddDays(1), 30, "Toothache",
            FundingSource.Private, null), TestData.Json);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        // The selected reason is carried onto the visit bill at check-in.
        var patient = await TestData.CreatePatientAsync(reception);
        var booked = await reception.PostAsJsonAsync("/api/appointments", new CreateAppointmentRequest(
            patient.PatientNumber, doctorId, DateTimeOffset.UtcNow.AddDays(1), 30, AppointmentReasons.Emergency,
            FundingSource.Private, null), TestData.Json);
        booked.EnsureSuccessStatusCode();
        var appointment = await booked.Content.ReadFromJsonAsync<AppointmentDto>(TestData.Json);
        var checkedIn = await TestData.CheckInAsync(reception, appointment!.Id);
        var bill = await reception.GetFromJsonAsync<BillDto>($"/api/billing/bills/{checkedIn.BillId}", TestData.Json);
        Assert.NotNull(bill);
        var feeLine = Assert.Single(bill!.Lines, x => x.Category == BillLineCategory.Appointment);
        Assert.Contains(AppointmentReasons.Emergency, feeLine.Description);
        Assert.Equal(20_000m, feeLine.UnitPrice);
    }

    [Fact]
    public async Task Bills_move_to_the_debt_ledger_with_the_patient_or_hmo_as_owner()
    {
        var reception = await TestData.ClientAsync(factory, "receptionist@clinic.local");
        var finance = await TestData.ClientAsync(factory, "finance@clinic.local");

        var patient = await TestData.CreatePatientAsync(reception);
        var created = await finance.PostAsJsonAsync("/api/billing/bills", new CreateManualBillRequest(
            patient.Id, null, null, "Debt test",
            [new CreateManualBillLineRequest(BillLineCategory.Consultation, "Consultation", 1m, 5_000m, null)]), TestData.Json);
        created.EnsureSuccessStatusCode();
        var bill = (await created.Content.ReadFromJsonAsync<ManualBillResultDto>(TestData.Json))!.Bill;

        var moved = await finance.PostAsJsonAsync($"/api/billing/bills/{bill.Id}/debt",
            new MoveBillToDebtRequest("Patient cannot pay today"), TestData.Json);
        moved.EnsureSuccessStatusCode();
        var debt = (await moved.Content.ReadFromJsonAsync<BillDto>(TestData.Json))!;
        Assert.True(debt.IsDebt);
        Assert.Equal(DebtOwner.Patient, debt.DebtOwner);
        Assert.Null(debt.DebtSettledAt);

        // Active debt leaves the main bills view and appears in the debt view.
        var main = await finance.GetFromJsonAsync<PagedResult<BillDto>>(
            "/api/billing/bills?inDebt=false&pageSize=100", TestData.Json);
        Assert.DoesNotContain(main!.Items, x => x.Id == bill.Id);
        var debts = await finance.GetFromJsonAsync<PagedResult<BillDto>>(
            "/api/billing/bills?inDebt=true&pageSize=100", TestData.Json);
        Assert.Contains(debts!.Items, x => x.Id == bill.Id);

        // Settling the balance closes the debt and removes it from the debt view.
        var payment = await finance.PostAsJsonAsync($"/api/billing/bills/{bill.Id}/payments",
            new PaymentRequest(debt.Balance, PaymentMethod.Cash, "DEBT-SETTLE", Guid.NewGuid().ToString(), null), TestData.Json);
        payment.EnsureSuccessStatusCode();
        var settled = (await payment.Content.ReadFromJsonAsync<BillDto>(TestData.Json))!;
        Assert.NotNull(settled.DebtSettledAt);
        var openDebts = await finance.GetFromJsonAsync<PagedResult<BillDto>>(
            "/api/billing/bills?inDebt=true&pageSize=100", TestData.Json);
        Assert.DoesNotContain(openDebts!.Items, x => x.Id == bill.Id);

        // An HMO bill is owed by the HMO, not the patient.
        var hmoId = await TestData.FirstHmoIdAsync(reception);
        var hmoPatient = await TestData.CreatePatientAsync(reception, hmoId);
        var hmoCreated = await finance.PostAsJsonAsync("/api/billing/bills", new CreateManualBillRequest(
            hmoPatient.Id, hmoId, null, "HMO debt test",
            [new CreateManualBillLineRequest(BillLineCategory.Consultation, "Consultation", 1m, 5_000m, null)]), TestData.Json);
        hmoCreated.EnsureSuccessStatusCode();
        var hmoBill = (await hmoCreated.Content.ReadFromJsonAsync<ManualBillResultDto>(TestData.Json))!.Bill;
        var hmoMoved = await finance.PostAsJsonAsync($"/api/billing/bills/{hmoBill.Id}/debt",
            new MoveBillToDebtRequest(null), TestData.Json);
        hmoMoved.EnsureSuccessStatusCode();
        var hmoDebt = (await hmoMoved.Content.ReadFromJsonAsync<BillDto>(TestData.Json))!;
        Assert.True(hmoDebt.IsDebt);
        Assert.Equal(DebtOwner.Hmo, hmoDebt.DebtOwner);
    }

    [Fact]
    public async Task External_credit_appears_in_the_ledger_and_updates_the_balance()
    {
        var finance = await TestData.ClientAsync(factory, "finance@clinic.local");
        var before = await finance.GetFromJsonAsync<AccountBookSummaryDto>(
            "/api/billing/account-book/summary", TestData.Json);
        Assert.NotNull(before);

        var reference = $"GRANT-{Guid.NewGuid():N}"[..20];
        var posted = await finance.PostAsJsonAsync("/api/billing/account-book/credits",
            new RecordCreditRequest(1_234.56m, "Equipment grant", DateTimeOffset.UtcNow,
                "External funds for monitors", reference), TestData.Json);
        posted.EnsureSuccessStatusCode();
        var credit = await posted.Content.ReadFromJsonAsync<AccountBookEntryDto>(TestData.Json);
        Assert.NotNull(credit);
        Assert.Equal("Credit", credit!.Direction);
        Assert.Equal("Equipment grant", credit.Source);
        Assert.Equal(1_234.56m, credit.Amount);

        var ledger = await finance.GetFromJsonAsync<PagedResult<AccountBookEntryDto>>(
            "/api/billing/account-book?pageSize=100", TestData.Json);
        Assert.Contains(ledger!.Items, x => x.Id == credit.Id && x.Source == "Equipment grant" && x.Reference == reference);

        var after = await finance.GetFromJsonAsync<AccountBookSummaryDto>(
            "/api/billing/account-book/summary", TestData.Json);
        Assert.NotNull(after);
        Assert.Equal(before!.TotalCredits + 1_234.56m, after!.TotalCredits);
        Assert.Equal(after.TotalCredits - after.TotalDebits, after.Balance);
    }
}
