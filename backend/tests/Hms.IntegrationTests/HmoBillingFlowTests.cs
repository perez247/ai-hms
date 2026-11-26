using System.Net;
using System.Net.Http.Json;
using Hms.Application.Common;
using Hms.Application.DTOs;

namespace Hms.IntegrationTests;

/// <summary>End-to-end API tests for HMO approval ordering, Finance settlement and authorization/validation.</summary>
public sealed class HmoBillingFlowTests(HmsApiFactory factory) : IClassFixture<HmsApiFactory>
{
    [Fact]
    public async Task Hmo_bill_is_payable_only_after_hmo_and_finance_approval()
    {
        var finance = await TestData.ClientAsync(factory, "finance@clinic.local");
        var pharmacy = await TestData.ClientAsync(factory, "pharmacy@clinic.local");
        var hmo = await TestData.ClientAsync(factory, "hmo@clinic.local");

        var (billId, encounterId, hmoId, enrollmentId) = await CreateHmoVisitAsync(pharmacy);
        var route = await finance.PostAsync($"/api/billing/bills/{billId}/route-hmo", null);
        route.EnsureSuccessStatusCode();

        var pa = await hmo.PostAsJsonAsync("/api/hmo/prior-authorizations", new SavePriorAuthorizationRequest(
            hmoId, enrollmentId, encounterId, null, $"PA-TEST-{Guid.NewGuid():N}", DateTimeOffset.UtcNow.AddDays(30), null, "Integration test"), TestData.Json);
        pa.EnsureSuccessStatusCode();

        var hmoApproval = await hmo.PostAsync($"/api/hmo/bills/{billId}/approve", null);
        Assert.True(hmoApproval.IsSuccessStatusCode, await hmoApproval.Content.ReadAsStringAsync());
        var afterHmo = await finance.GetFromJsonAsync<BillDto>($"/api/billing/bills/{billId}", TestData.Json);
        Assert.NotNull(afterHmo);
        Assert.Equal("HmoApproved", afterHmo.Status.ToString());
        Assert.Null(afterHmo.FinanceApprovedAt);

        var payment = await finance.PostAsJsonAsync($"/api/billing/bills/{billId}/payments", new PaymentRequest(
            afterHmo.PatientPayableTotal, Hms.Domain.Enums.PaymentMethod.Cash, "TEST-CASH", Guid.NewGuid().ToString(), null), TestData.Json);
        payment.EnsureSuccessStatusCode();

        var approval = await finance.PostAsJsonAsync($"/api/billing/bills/{billId}/approve", new ApproveBillRequest("Integration test"), TestData.Json);
        approval.EnsureSuccessStatusCode();
        var final = await approval.Content.ReadFromJsonAsync<BillDto>(TestData.Json);
        Assert.NotNull(final);
        Assert.Equal("Approved", final.Status.ToString());
        Assert.NotNull(final.HmoApprovedAt);
        Assert.NotNull(final.FinanceApprovedAt);
        Assert.NotNull(final.Receivable);
        Assert.Equal(final.HmoPayableTotal, final.Receivable!.Amount);

        var ledger = await finance.GetFromJsonAsync<PagedResult<AccountBookEntryDto>>("/api/billing/account-book?pageSize=100", TestData.Json);
        Assert.Contains(ledger!.Items, x => x.Direction == "Credit" && x.PaymentId is not null);
    }

    [Fact]
    public async Task Invalid_payment_returns_consistent_validation_problem()
    {
        var finance = await TestData.ClientAsync(factory, "finance@clinic.local");
        var pharmacy = await TestData.ClientAsync(factory, "pharmacy@clinic.local");
        var (billId, _, _, _) = await CreateHmoVisitAsync(pharmacy);
        var response = await finance.PostAsJsonAsync($"/api/billing/bills/{billId}/payments",
            new PaymentRequest(0, Hms.Domain.Enums.PaymentMethod.Cash, null, null, null), TestData.Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var document = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Validation failed", document.RootElement.GetProperty("title").GetString());
        Assert.True(document.RootElement.GetProperty("errors").TryGetProperty("Request.Amount", out _));
    }

    [Fact]
    public async Task Pharmacy_can_route_and_approve_an_hmo_bill_after_hmo_requirements()
    {
        var pharmacy = await TestData.ClientAsync(factory, "pharmacy@clinic.local");
        var hmo = await TestData.ClientAsync(factory, "hmo@clinic.local");

        // HMO read access now comes with the Pharmacy role.
        var directory = await pharmacy.GetAsync("/api/hmo/companies");
        Assert.Equal(HttpStatusCode.OK, directory.StatusCode);

        var (billId, encounterId, hmoId, enrollmentId) = await CreateHmoVisitAsync(pharmacy);

        var route = await pharmacy.PostAsync($"/api/billing/bills/{billId}/route-hmo", null);
        route.EnsureSuccessStatusCode();

        var pa = await hmo.PostAsJsonAsync("/api/hmo/prior-authorizations", new SavePriorAuthorizationRequest(
            hmoId, enrollmentId, encounterId, null, $"PA-PHARM-{Guid.NewGuid():N}", DateTimeOffset.UtcNow.AddDays(30), null, "Pharmacy approval test"), TestData.Json);
        pa.EnsureSuccessStatusCode();
        (await hmo.PostAsync($"/api/hmo/bills/{billId}/approve", null)).EnsureSuccessStatusCode();

        var afterHmo = await pharmacy.GetFromJsonAsync<BillDto>($"/api/billing/bills/{billId}", TestData.Json);
        Assert.NotNull(afterHmo);
        var payment = await pharmacy.PostAsJsonAsync($"/api/billing/bills/{billId}/payments", new PaymentRequest(
            afterHmo.PatientPayableTotal, Hms.Domain.Enums.PaymentMethod.Cash, "PHARMACY-COPAY", Guid.NewGuid().ToString(), null), TestData.Json);
        payment.EnsureSuccessStatusCode();

        var approval = await pharmacy.PostAsJsonAsync($"/api/billing/bills/{billId}/approve", new ApproveBillRequest("Pharmacy approval"), TestData.Json);
        approval.EnsureSuccessStatusCode();
        var final = await approval.Content.ReadFromJsonAsync<BillDto>(TestData.Json);
        Assert.NotNull(final);
        Assert.Equal("Approved", final.Status.ToString());
        Assert.NotNull(final.FinanceApprovedAt);
        Assert.NotNull(final.Receivable);
    }

    [Fact]
    public async Task Finance_can_read_prior_authorizations()
    {
        var finance = await TestData.ClientAsync(factory, "finance@clinic.local");
        var response = await finance.GetAsync("/api/hmo/prior-authorizations?page=1&pageSize=5");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<(Guid BillId, Guid EncounterId, Guid HmoId, Guid EnrollmentId)> CreateHmoVisitAsync(
        HttpClient pharmacy)
    {
        var reception = await TestData.ClientAsync(factory, "receptionist@clinic.local");
        var doctor = await TestData.ClientAsync(factory, "doctor@clinic.local");
        var hmoId = await TestData.FirstHmoIdAsync(reception);
        var (patient, _, encounter, billId) = await TestData.CreateOutpatientVisitAsync(
            reception, doctor, hmoId, Hms.Domain.Enums.FundingSource.Hmo);
        var enrollments = await doctor.GetFromJsonAsync<List<HmoEnrollmentOptionDto>>(
            $"/api/reference/patients/{patient.Id}/hmo-enrollments", TestData.Json);
        var enrollment = Assert.Single(enrollments!);

        var medicationId = await TestData.FirstMedicationIdAsync(doctor);
        await TestData.PrescribeAsync(doctor, encounter.Id, medicationId, 3);
        // Pharmacy may only commit an outpatient bill after the doctor signs the encounter.
        await TestData.SignEncounterAsync(doctor, encounter.Id);

        var prescription = await pharmacy.GetFromJsonAsync<PharmacyPrescriptionDto>(
            $"/api/pharmacy/encounters/{encounter.Id}/prescription", TestData.Json);
        Assert.NotNull(prescription);
        var dispense = await pharmacy.PostAsJsonAsync("/api/pharmacy/dispenses", new DispenseRequest(prescription!.Id,
            prescription.Items.Select(x => new DispenseItemRequest(x.Id, x.QuantityPrescribed)).ToList()), TestData.Json);
        if (!dispense.IsSuccessStatusCode)
            Assert.Fail($"{dispense.StatusCode}: {await dispense.Content.ReadAsStringAsync()}");
        return (billId, encounter.Id, hmoId, enrollment.Id);
    }
}
