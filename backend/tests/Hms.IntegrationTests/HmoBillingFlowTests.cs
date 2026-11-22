using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Hms.IntegrationTests;

/// <summary>End-to-end API tests for HMO approval ordering, Finance settlement and authorization/validation.</summary>
public sealed class HmoBillingFlowTests(HmsApiFactory factory) : IClassFixture<HmsApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task Hmo_bill_is_payable_only_after_hmo_and_finance_approval()
    {
        var finance = await ClientAsync("finance@clinic.local");
        var pharmacy = await ClientAsync("pharmacy@clinic.local");
        var hmo = await ClientAsync("hmo@clinic.local");

        var (billId, encounterId, hmoId, enrollmentId) = await SeedIdsAsync();
        var prescription = await pharmacy.GetFromJsonAsync<PharmacyPrescriptionDto>($"/api/pharmacy/encounters/{encounterId}/prescription", Json);
        Assert.NotNull(prescription);
        var dispense = await pharmacy.PostAsJsonAsync("/api/pharmacy/dispenses", new DispenseRequest(prescription.Id,
            prescription.Items.Select(x => new DispenseItemRequest(x.Id, x.QuantityPrescribed)).ToList()));
        if (!dispense.IsSuccessStatusCode) Assert.Fail($"{dispense.StatusCode}: {await dispense.Content.ReadAsStringAsync()}");
        var dispenseResult = await dispense.Content.ReadFromJsonAsync<DispenseResultDto>(Json);
        Assert.NotNull(dispenseResult);
        Assert.Equal("Committed", dispenseResult.BillStatus);

        var route = await finance.PostAsync($"/api/billing/bills/{billId}/route-hmo", null);
        route.EnsureSuccessStatusCode();

        var pa = await hmo.PostAsJsonAsync("/api/hmo/prior-authorizations", new SavePriorAuthorizationRequest(
            hmoId, enrollmentId, encounterId, null, $"PA-TEST-{Guid.NewGuid():N}", DateTimeOffset.UtcNow.AddDays(30), null, "Integration test"));
        pa.EnsureSuccessStatusCode();

        var hmoApproval = await hmo.PostAsync($"/api/hmo/bills/{billId}/approve", null);
        hmoApproval.EnsureSuccessStatusCode();
        var afterHmo = await finance.GetFromJsonAsync<BillDto>($"/api/billing/bills/{billId}", Json);
        Assert.NotNull(afterHmo);
        Assert.Equal("HmoApproved", afterHmo.Status.ToString());
        Assert.Null(afterHmo.FinanceApprovedAt);

        var payment = await finance.PostAsJsonAsync($"/api/billing/bills/{billId}/payments", new PaymentRequest(
            afterHmo.PatientPayableTotal, Hms.Domain.Enums.PaymentMethod.Cash, "TEST-CASH", Guid.NewGuid().ToString(), null));
        payment.EnsureSuccessStatusCode();

        var approval = await finance.PostAsJsonAsync($"/api/billing/bills/{billId}/approve", new ApproveBillRequest("Integration test"));
        approval.EnsureSuccessStatusCode();
        var final = await approval.Content.ReadFromJsonAsync<BillDto>(Json);
        Assert.NotNull(final);
        Assert.Equal("Approved", final.Status.ToString());
        Assert.NotNull(final.HmoApprovedAt);
        Assert.NotNull(final.FinanceApprovedAt);
        Assert.NotNull(final.Receivable);
        Assert.Equal(final.HmoPayableTotal, final.Receivable!.Amount);

        var ledger = await finance.GetFromJsonAsync<PagedResult<AccountBookEntryDto>>("/api/billing/account-book?pageSize=100", Json);
        Assert.Contains(ledger!.Items, x => x.Direction == "Credit" && x.PaymentId is not null);
    }


    [Fact]
    public async Task Invalid_payment_returns_consistent_validation_problem()
    {
        var finance = await ClientAsync("finance@clinic.local");
        var (billId, _, _, _) = await SeedIdsAsync();
        var response = await finance.PostAsJsonAsync($"/api/billing/bills/{billId}/payments",
            new PaymentRequest(0, Hms.Domain.Enums.PaymentMethod.Cash, null, null, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Validation failed", document.RootElement.GetProperty("title").GetString());
        Assert.True(document.RootElement.GetProperty("errors").TryGetProperty("Request.Amount", out _));
    }

    [Fact]
    public async Task Pharmacy_cannot_approve_a_bill()
    {
        var pharmacy = await ClientAsync("pharmacy@clinic.local");
        var (billId, _, _, _) = await SeedIdsAsync();
        var response = await pharmacy.PostAsJsonAsync($"/api/billing/bills/{billId}/approve", new ApproveBillRequest(null));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<HttpClient> ClientAsync(string email)
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "ChangeMe123!"));
        login.EnsureSuccessStatusCode();
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>(Json);
        Assert.NotNull(auth);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }

    private async Task<(Guid BillId, Guid EncounterId, Guid HmoId, Guid EnrollmentId)> SeedIdsAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HmsDbContext>();
        var encounter = db.Encounters.Single(x => x.EncounterNumber == "ENC-DEMO-0001");
        var bill = db.Bills.Single(x => x.BillNumber == "BILL-DEMO-0002");
        var hmo = db.HmoCompanies.Single();
        var enrollment = db.PatientHmoEnrollments.Single();
        return await Task.FromResult((bill.Id, encounter.Id, hmo.Id, enrollment.Id));
    }
}
