using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Domain.Enums;

namespace Hms.IntegrationTests;

/// <summary>Covers nurse patient search/vitals, encounter administration billing and ledger debits.</summary>
public sealed class NurseAndLedgerFlowTests(HmsApiFactory factory) : IClassFixture<HmsApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task Nurse_can_search_patient_and_record_patient_level_vitals()
    {
        var nurse = await ClientAsync("nurse@clinic.local");
        var search = await nurse.GetFromJsonAsync<PagedResult<PatientSearchItem>>("/api/patients?search=Ada", Json);
        Assert.NotNull(search);
        Assert.Contains(search.Items, x => x.PatientNumber == "PAT-DEMO-0001");

        var request = new RecordPatientVitalsRequest("PAT-DEMO-0001", 36.8m, 120, 80, 72, 16, 99, 68m, 165m, 1, "Nurse station");
        var record = await nurse.PostAsJsonAsync("/api/nurse/patients/PAT-DEMO-0001/vitals", request, Json);
        record.EnsureSuccessStatusCode();

        var page = await nurse.GetFromJsonAsync<PatientVitalsPageDto>("/api/nurse/patients/PAT-DEMO-0001/vitals", Json);
        Assert.NotNull(page);
        Assert.True(page.Total >= 1);
    }

    [Fact]
    public async Task Administered_inpatient_prescription_creates_a_billing_line()
    {
        var nurse = await ClientAsync("nurse@clinic.local");
        var pharmacy = await ClientAsync("pharmacy@clinic.local");
        var finance = await ClientAsync("finance@clinic.local");

        var encounters = await nurse.GetFromJsonAsync<List<NurseEncounterDto>>("/api/nurse/patients/PAT-DEMO-0002/encounters", Json);
        var encounter = Assert.Single(encounters!);
        var prescription = await pharmacy.GetFromJsonAsync<PharmacyPrescriptionDto>(
            $"/api/pharmacy/encounters/{encounter.Id}/prescription", Json);
        Assert.NotNull(prescription);

        var dispense = await pharmacy.PostAsJsonAsync("/api/pharmacy/dispenses", new DispenseRequest(prescription.Id,
            prescription.Items.Select(x => new DispenseItemRequest(x.Id, x.QuantityPrescribed)).ToList()), Json);
        dispense.EnsureSuccessStatusCode();

        var refreshed = await nurse.GetFromJsonAsync<List<NurseEncounterDto>>("/api/nurse/patients/PAT-DEMO-0002/encounters", Json);
        var item = refreshed!.SelectMany(x => x.Prescriptions).Single(x => x.CanAdminister);
        var administer = await nurse.PostAsJsonAsync("/api/clinical/administrations", new RecordAdministrationRequest(
            item.DispenseItemId!.Value, 1m, item.Route, null, "Nurse test"), Json);
        administer.EnsureSuccessStatusCode();

        var basic = await nurse.GetFromJsonAsync<PatientBasicProfileDto>("/api/patients/number/PAT-DEMO-0002", Json);
        var bills = await finance.GetFromJsonAsync<PagedResult<BillDto>>($"/api/billing/bills?patientId={basic!.Patient.Id}", Json);
        Assert.Contains(bills!.Items.SelectMany(x => x.Lines),
            line => line.Category == BillLineCategory.Medication);
    }

    [Fact]
    public async Task Handover_endpoint_is_nurse_only_and_allows_page_size_100()
    {
        var nurse = await ClientAsync("nurse@clinic.local");
        var doctor = await ClientAsync("doctor@clinic.local");

        var allowed = await nurse.GetAsync("/api/nurse/handovers?page=1&pageSize=100");
        Assert.Equal(System.Net.HttpStatusCode.OK, allowed.StatusCode);
        var page = await allowed.Content.ReadFromJsonAsync<List<ShiftHandoverDto>>(Json);
        Assert.NotNull(page);

        var denied = await doctor.GetAsync("/api/nurse/handovers?page=1&pageSize=100");
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, denied.StatusCode);
    }

    [Fact]
    public async Task Triage_lists_pending_appointments_and_active_inpatients()
    {
        var nurse = await ClientAsync("nurse@clinic.local");
        var appointments = await nurse.GetAsync("/api/nurse/appointments");
        Assert.Equal(System.Net.HttpStatusCode.OK, appointments.StatusCode);
        var inpatients = await nurse.GetAsync("/api/nurse/inpatients");
        Assert.Equal(System.Net.HttpStatusCode.OK, inpatients.StatusCode);
    }

    [Fact]
    public async Task Purchase_posts_a_debit_to_the_ledger_with_reference()
    {
        var finance = await ClientAsync("finance@clinic.local");
        var response = await finance.PostAsJsonAsync("/api/billing/account-book/purchases",
            new AccountBookEntryRequest(Hms.Domain.Enums.AccountEntryType.Expense, "Supplies", 250m,
                "Examination gloves", DateTimeOffset.UtcNow, "INV-LEDGER-1"), Json);
        response.EnsureSuccessStatusCode();
        var ledger = await finance.GetFromJsonAsync<PagedResult<AccountBookEntryDto>>("/api/billing/account-book?pageSize=100", Json);
        Assert.Contains(ledger!.Items, x => x.Direction == "Debit" && x.Reference == "INV-LEDGER-1");
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
}
