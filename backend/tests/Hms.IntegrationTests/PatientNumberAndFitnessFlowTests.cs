using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hms.Application.DTOs;
using Hms.Domain.Enums;

namespace Hms.IntegrationTests;

/// <summary>Verifies patient-number lookup and the Reception→Nurse→Doctor→Finance fitness gates.</summary>
public sealed class PatientNumberAndFitnessFlowTests(HmsApiFactory factory) : IClassFixture<HmsApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task Patient_number_resolves_profile_and_encounters()
    {
        var nurse = await ClientAsync("nurse@clinic.local");
        var basic = await nurse.GetFromJsonAsync<PatientBasicProfileDto>("/api/patients/number/PAT-DEMO-0001", Json);
        Assert.NotNull(basic);
        Assert.Equal("PAT-DEMO-0001", basic.Patient.PatientNumber);
        Assert.Equal("Ada Okafor", basic.Patient.FullName);
        Assert.True(basic.IsPrivate == false);
        Assert.Empty(basic.Patient.HmoEnrollments); // HMO assignments are lazy-loaded separately.

        var encounters = await nurse.GetFromJsonAsync<List<PatientEncounterSummaryDto>>("/api/patients/number/PAT-DEMO-0001/encounters", Json);
        Assert.NotNull(encounters);
        Assert.Contains(encounters, x => x.EncounterNumber == "ENC-DEMO-0001");
    }

    [Fact]
    public async Task Fitness_stage_order_is_enforced_and_private_payment_settles()
    {
        var reception = await ClientAsync("receptionist@clinic.local");
        var nurse = await ClientAsync("nurse@clinic.local");
        var doctor = await ClientAsync("doctor@clinic.local");
        var finance = await ClientAsync("finance@clinic.local");

        var create = await reception.PostAsJsonAsync("/api/fitness", new CreateFitnessCertificateRequest(
            "PAT-DEMO-0002", "Chinedu Bello", 42, Sex.Male, "+234-802-000-0001",
            FitnessPaymentType.Private, null, null, DateOnly.FromDateTime(DateTime.UtcNow), null), Json);
        create.EnsureSuccessStatusCode();
        var record = await create.Content.ReadFromJsonAsync<FitnessCertificateDto>(Json);
        Assert.NotNull(record);
        Assert.Equal(FitnessStatus.AwaitingNurse, record.Status);

        // Doctor cannot jump the queue before Nurse submits.
        var premature = await doctor.PostAsJsonAsync($"/api/fitness/{record.Id}/decision",
            new FitnessDecisionRequest(true, null), Json);
        Assert.Equal(HttpStatusCode.Conflict, premature.StatusCode);

        var clinical = await nurse.PostAsJsonAsync($"/api/fitness/{record.Id}/clinical", new UpdateFitnessClinicalInfoRequest(
            [
                new("Height", "178cm"), new("Weight", "82kg"),
                new("Blood pressure (BP)", "126/82"), new("Pulse", "84")
            ]), Json);
        clinical.EnsureSuccessStatusCode();

        using var multipart = new MultipartFormDataContent();
        var file = new ByteArrayContent(Encoding.UTF8.GetBytes("signed certificate"));
        file.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        multipart.Add(file, "file", "fitness.txt");
        var upload = await nurse.PostAsync($"/api/fitness/{record.Id}/certificate", multipart);
        upload.EnsureSuccessStatusCode();

        var submit = await nurse.PostAsync($"/api/fitness/{record.Id}/submit", null);
        submit.EnsureSuccessStatusCode();

        var decision = await doctor.PostAsJsonAsync($"/api/fitness/{record.Id}/decision",
            new FitnessDecisionRequest(true, null), Json);
        decision.EnsureSuccessStatusCode();
        var approved = await decision.Content.ReadFromJsonAsync<FitnessCertificateDto>(Json);
        Assert.Equal(FitnessStatus.Approved, approved!.Status);

        var payment = await finance.PostAsJsonAsync($"/api/fitness/{record.Id}/payment",
            new FitnessPaymentRequest(true), Json);
        payment.EnsureSuccessStatusCode();
        var paid = await payment.Content.ReadFromJsonAsync<FitnessCertificateDto>(Json);
        Assert.Equal(FitnessPaymentStatus.Paid, paid!.PaymentStatus);
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
