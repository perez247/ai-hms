using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Domain.Enums;

namespace Hms.IntegrationTests;

/// <summary>Covers doctor queues, unlinked fitness/filters, HMO tariffs and tariff-driven pricing.</summary>
public sealed class NewWorkflowFeaturesTests(HmsApiFactory factory) : IClassFixture<HmsApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task Doctor_queue_lists_checked_in_patients_and_clears_after_encounter()
    {
        var reception = await ClientAsync("receptionist@clinic.local");
        var doctor = await ClientAsync("doctor@clinic.local");

        var patient = await doctor.GetFromJsonAsync<PatientBasicProfileDto>("/api/patients/number/PAT-DEMO-0002", Json);
        Assert.NotNull(patient);
        var doctorId = (await doctor.GetFromJsonAsync<MePayload>("/api/auth/me", Json))!.Id;
        var appointment = await reception.PostAsJsonAsync("/api/appointments", new CreateAppointmentRequest(
            patient.Patient.PatientNumber, doctorId, DateTimeOffset.UtcNow.AddHours(1), 30, "Queue test"), Json);
        appointment.EnsureSuccessStatusCode();
        var created = await appointment.Content.ReadFromJsonAsync<AppointmentDto>(Json);
        Assert.NotNull(created);
        (await reception.PostAsync($"/api/appointments/{created.Id}/check-in", null)).EnsureSuccessStatusCode();

        var queue = await doctor.GetFromJsonAsync<DoctorQueueDto>("/api/clinical/doctor-queue", Json);
        Assert.NotNull(queue);
        Assert.Contains(queue.AwaitingOutpatients, x => x.SourceId == created.Id && x.PatientNumber == "PAT-DEMO-0002");
        Assert.Contains(queue.ActiveInpatients, x => x.ReferenceNumber == "ADM-DEMO-0001");

        var encounter = await doctor.PostAsJsonAsync("/api/clinical/encounters/outpatient", new CreateOutpatientEncounterRequest(
            patient.Patient.Id, created.DoctorId, created.Id, false, null, "Queue test encounter"), Json);
        encounter.EnsureSuccessStatusCode();

        var after = await doctor.GetFromJsonAsync<DoctorQueueDto>("/api/clinical/doctor-queue", Json);
        Assert.NotNull(after);
        Assert.DoesNotContain(after.AwaitingOutpatients, x => x.SourceId == created.Id);
    }

    [Fact]
    public async Task Fitness_allows_unlinked_creation_and_combined_filters()
    {
        var reception = await ClientAsync("receptionist@clinic.local");
        var nurse = await ClientAsync("nurse@clinic.local");
        var doctor = await ClientAsync("doctor@clinic.local");
        var finance = await ClientAsync("finance@clinic.local");
        const string reference = "FREE-ENTRY-QUEUE-TEST";

        var create = await reception.PostAsJsonAsync("/api/fitness", new CreateFitnessCertificateRequest(
            reference, "Unlinked Person", 33, Sex.Other, "+234-000-queue",
            FitnessPaymentType.Private, null, null, DateOnly.FromDateTime(DateTime.UtcNow), null), Json);
        create.EnsureSuccessStatusCode();
        var record = await create.Content.ReadFromJsonAsync<FitnessCertificateDto>(Json);
        Assert.NotNull(record);
        Assert.Null(record.PatientId);
        Assert.Equal(reference, record.PatientNumber);

        var clinical = await nurse.PostAsJsonAsync($"/api/fitness/{record.Id}/clinical", new UpdateFitnessClinicalInfoRequest(
            [new("BP", "120/80")]), Json);
        clinical.EnsureSuccessStatusCode();
        (await nurse.PostAsync($"/api/fitness/{record.Id}/submit", null)).EnsureSuccessStatusCode();
        (await doctor.PostAsJsonAsync($"/api/fitness/{record.Id}/decision", new FitnessDecisionRequest(true, null), Json)).EnsureSuccessStatusCode();
        (await finance.PostAsJsonAsync($"/api/fitness/{record.Id}/payment", new FitnessPaymentRequest(true), Json)).EnsureSuccessStatusCode();

        var filtered = await finance.GetFromJsonAsync<PagedResult<FitnessCertificateDto>>(
            $"/api/fitness?status=Approved&paymentType=Private&paymentStatus=Paid&search={reference}", Json);
        Assert.NotNull(filtered);
        Assert.Contains(filtered.Items, x => x.Id == record.Id);

        var noMatch = await finance.GetFromJsonAsync<PagedResult<FitnessCertificateDto>>(
            $"/api/fitness?status=AwaitingNurse&paymentType=Private&paymentStatus=Paid&search={reference}", Json);
        Assert.NotNull(noMatch);
        Assert.Empty(noMatch.Items);
    }

    [Fact]
    public async Task Hmo_officer_registers_tariff_and_finance_applies_negotiated_price()
    {
        var hmoOfficer = await ClientAsync("hmo@clinic.local");
        var doctor = await ClientAsync("doctor@clinic.local");
        var finance = await ClientAsync("finance@clinic.local");
        var pharmacist = await ClientAsync("pharmacy@clinic.local");

        var forbidden = await doctor.PostAsJsonAsync("/api/hmo/companies", new
        {
            code = "NOPE", name = "Should fail", contactPerson = "", email = "", phone = "", address = "",
            contractTerms = "", initialTariff = (object?)null
        }, Json);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        var code = $"REG-TARIFF-{Guid.NewGuid():N}".ToUpperInvariant()[..24];
        var registration = await hmoOfficer.PostAsJsonAsync("/api/hmo/companies", new
        {
            code, name = "Registration Test HMO", contactPerson = "Tester", email = "test@example.test",
            phone = "123", address = "Test address", contractTerms = "Test contract",
            initialTariff = new
            {
                name = "Registration tariff", effectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow), effectiveTo = (DateOnly?)null,
                items = new[]
                {
                    new { code = "TESTDRUG", description = "Test drug", category = "Medication", unit = "tablet", unitPrice = 100m, coveragePercent = 50m, requiresPriorAuthorization = false }
                }
            }
        }, Json);
        registration.EnsureSuccessStatusCode();
        var directory = await hmoOfficer.GetFromJsonAsync<HmoDirectoryEntryDto[]>("api/hmo/companies", Json);
        var hmo = Assert.Single(directory!, x => x.Code == code);
        var tariff = Assert.Single(hmo.Tariffs);
        Assert.Equal(TariffStatus.Active, tariff.Status);
        Assert.Equal(100m, Assert.Single(tariff.Items).UnitPrice);

        var patient = await finance.GetFromJsonAsync<PatientBasicProfileDto>("/api/patients/number/PAT-DEMO-0001", Json);
        Assert.NotNull(patient);
        var bill = await finance.PostAsJsonAsync("/api/billing/bills", new CreateManualBillRequest(
            patient.Patient.Id, hmo.Id, null, "Tariff pricing test",
            [
                new CreateManualBillLineRequest(BillLineCategory.Medication, "Test drug", 2m, 500m, "TESTDRUG"),
                new CreateManualBillLineRequest(BillLineCategory.Medication, "Unmatched drug", 1m, 250m, "UNKNOWN-CODE")
            ]), Json);
        bill.EnsureSuccessStatusCode();
        var result = await bill.Content.ReadFromJsonAsync<ManualBillResultDto>(Json);
        Assert.NotNull(result);
        var priced = Assert.Single(result.Bill.Lines, x => x.Description == "Test drug");
        Assert.Equal(100m, priced.UnitPrice);
        Assert.Equal(100m, priced.HmoAmount);
        Assert.Equal(100m, priced.PatientAmount);
        Assert.Contains(result.Warnings, x => x.Contains("UNKNOWN-CODE"));

        var encounters = await pharmacist.GetFromJsonAsync<List<PatientEncounterSummaryDto>>(
            "/api/patients/number/PAT-DEMO-0001/encounters", Json);
        var encounter = Assert.Single(encounters!, x => x.EncounterNumber == "ENC-DEMO-0001");
        var options = await pharmacist.GetFromJsonAsync<PrescriptionPriceOptionsDto>(
            $"/api/pharmacy/encounters/{encounter.Id}/price-options", Json);
        Assert.NotNull(options);
        var privateOption = Assert.Single(options.Options, x => x.PayerType == "Private");
        var hmoOption = Assert.Single(options.Options, x => x.HmoCompanyId is not null);
        Assert.Equal(9m * 500m + 10m * 1200m, privateOption.TotalPrice);
        Assert.True(hmoOption.Undercovered);
        Assert.Contains(hmoOption.Warnings, x => x.Contains("below market"));
    }

    private sealed record MePayload(Guid Id, string FullName, string Role);

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
