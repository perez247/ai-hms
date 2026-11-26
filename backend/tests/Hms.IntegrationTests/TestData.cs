using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hms.Application.DTOs;
using Hms.Domain.Enums;

namespace Hms.IntegrationTests;

/// <summary>
/// Creates the patient/appointment/encounter records each test needs through the public API.
/// The development seeder no longer inserts demo patients, so tests must build their own data.
/// </summary>
internal static class TestData
{
    public const string Password = "ChangeMe123!";

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task<HttpClient> ClientAsync(HmsApiFactory factory, string email)
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, Password), Json);
        login.EnsureSuccessStatusCode();
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>(Json);
        if (auth is null) throw new InvalidOperationException($"Could not sign in {email}.");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }

    public static async Task<Guid> CurrentUserIdAsync(HttpClient client)
    {
        var me = await client.GetFromJsonAsync<MeResponse>("/api/auth/me", Json);
        return me?.Id ?? throw new InvalidOperationException("Could not read the current user.");
    }

    public static async Task<Guid> FirstHmoIdAsync(HttpClient client)
    {
        var hmos = await client.GetFromJsonAsync<List<HmoOptionDto>>("/api/reference/hmos", Json);
        return hmos?.FirstOrDefault()?.Id ?? throw new InvalidOperationException("No HMO company is configured.");
    }

    public static async Task<PatientDto> CreatePatientAsync(HttpClient reception, Guid? hmoId = null,
        string firstName = "Test")
    {
        var unique = Guid.NewGuid().ToString("N")[..10];
        var enrollments = hmoId is null
            ? null
            : new[]
            {
                new CreateHmoEnrollmentRequest(hmoId.Value, $"MEM-{unique}", "Standard",
                    DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
                    DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)), true)
            };
        var request = new CreatePatientRequest(firstName, null, $"Patient{unique}", Sex.Female,
            new DateOnly(1990, 1, 1), $"+234{unique}", $"{unique}@example.test", "1 Test Street",
            "Lagos", "Ikeja", null, "Tester", "Next of Kin", "+234-000", enrollments);
        var response = await reception.PostAsJsonAsync("/api/patients", request, Json);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Create patient failed: {response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        return (await response.Content.ReadFromJsonAsync<PatientDto>(Json))!;
    }

    public static async Task<AppointmentDto> BookAsync(HttpClient reception, string patientNumber, Guid doctorId,
        FundingSource funding = FundingSource.Private, Guid? enrollmentId = null)
    {
        var response = await reception.PostAsJsonAsync("/api/appointments", new CreateAppointmentRequest(
            patientNumber, doctorId, DateTimeOffset.UtcNow.AddHours(1), 30, AppointmentReasons.Consultation,
            funding, enrollmentId), Json);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Book appointment failed: {response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        return (await response.Content.ReadFromJsonAsync<AppointmentDto>(Json))!;
    }

    public static async Task<AppointmentDto> CheckInAsync(HttpClient reception, Guid appointmentId)
    {
        var response = await reception.PostAsync($"/api/appointments/{appointmentId}/check-in", null);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Check-in failed: {response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        return (await response.Content.ReadFromJsonAsync<AppointmentDto>(Json))!;
    }

    public static async Task<EncounterDto> CreateOutpatientEncounterAsync(HttpClient doctor, PatientDto patient,
        AppointmentDto appointment)
    {
        var response = await doctor.PostAsJsonAsync("/api/clinical/encounters/outpatient",
            new CreateOutpatientEncounterRequest(patient.Id, appointment.DoctorId, appointment.Id, false,
                appointment.HmoEnrollmentId, "Integration test"), Json);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Create encounter failed: {response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        return (await response.Content.ReadFromJsonAsync<EncounterDto>(Json))!;
    }

    public static async Task<(AdmissionDto Admission, EncounterDto Encounter)> CreateInpatientAsync(
        HttpClient doctor, PatientDto patient)
    {
        var doctorId = await CurrentUserIdAsync(doctor);
        var admissionResponse = await doctor.PostAsJsonAsync("/api/clinical/admissions",
            new CreateAdmissionRequest(patient.Id, doctorId, WardType.General, "G-01", null,
                "Integration test", null), Json);
        if (!admissionResponse.IsSuccessStatusCode)
            throw new InvalidOperationException($"Create admission failed: {admissionResponse.StatusCode} {await admissionResponse.Content.ReadAsStringAsync()}");
        var admission = (await admissionResponse.Content.ReadFromJsonAsync<AdmissionDto>(Json))!;
        var encounterResponse = await doctor.PostAsJsonAsync($"/api/clinical/admissions/{admission.Id}/encounters",
            new CreateInpatientEncounterRequest(admission.Id, doctorId, "Integration test"), Json);
        if (!encounterResponse.IsSuccessStatusCode)
            throw new InvalidOperationException($"Create inpatient encounter failed: {encounterResponse.StatusCode} {await encounterResponse.Content.ReadAsStringAsync()}");
        var encounter = (await encounterResponse.Content.ReadFromJsonAsync<EncounterDto>(Json))!;
        return (admission, encounter);
    }

    public static async Task PrescribeAsync(HttpClient doctor, Guid encounterId, Guid medicationId,
        decimal quantity = 3)
    {
        var response = await doctor.PostAsJsonAsync("/api/clinical/prescriptions",
            new CreatePrescriptionRequest(encounterId, null,
            [
                new PrescriptionItemRequest(medicationId, "1 tablet", "Oral", "BD", 3, quantity, false, null)
            ]), Json);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Create prescription failed: {response.StatusCode} {await response.Content.ReadAsStringAsync()}");
    }

    /// <summary>Signs (completes) an encounter with the minimum data the API requires.</summary>
    public static async Task<EncounterDto> SignEncounterAsync(HttpClient doctor, Guid encounterId)
    {
        var response = await doctor.PutAsJsonAsync($"/api/clinical/encounters/{encounterId}/clerking",
            new SaveEncounterRequest("Integration test complaint", "Two days of symptoms", null, null,
                "R69", "Discharge", null, true), Json);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Sign encounter failed: {response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        return (await response.Content.ReadFromJsonAsync<EncounterDto>(Json))!;
    }

    public static async Task<Guid> FirstMedicationIdAsync(HttpClient doctor)
    {
        var items = await doctor.GetFromJsonAsync<List<StockItemDto>>("/api/reference/stock-items?type=Medication", Json);
        if (items is null || items.Count == 0)
            throw new InvalidOperationException("No medication stock item is configured.");
        // Prefer a medication covered by the seeded HMO tariff so HMO approval paths stay exercisable.
        return (items.FirstOrDefault(x => x.Code == "PARA500") ?? items[0]).Id;
    }

    public static async Task<Guid> StockItemIdAsync(HttpClient doctor, string type, string code)
    {
        var items = await doctor.GetFromJsonAsync<List<StockItemDto>>($"/api/reference/stock-items?type={type}", Json);
        return (items ?? []).FirstOrDefault(x => x.Code == code)?.Id
            ?? throw new InvalidOperationException($"Stock item {type}/{code} is not configured.");
    }

    /// <summary>Books, checks in, opens an outpatient encounter and returns the single visit bill id.</summary>
    public static async Task<(PatientDto Patient, AppointmentDto Appointment, EncounterDto Encounter, Guid BillId)>
        CreateOutpatientVisitAsync(HttpClient reception, HttpClient doctor, Guid? hmoId = null,
            FundingSource funding = FundingSource.Private)
    {
        var patient = await CreatePatientAsync(reception, hmoId);
        var doctorId = await CurrentUserIdAsync(doctor);
        Guid? enrollmentId = null;
        if (hmoId is not null && funding == FundingSource.Hmo)
        {
            var enrollments = await doctor.GetFromJsonAsync<List<HmoEnrollmentOptionDto>>(
                $"/api/reference/patients/{patient.Id}/hmo-enrollments", Json);
            enrollmentId = enrollments?.Single().Id;
        }
        var appointment = await BookAsync(reception, patient.PatientNumber, doctorId, funding, enrollmentId);
        var checkedIn = await CheckInAsync(reception, appointment.Id);
        var encounter = await CreateOutpatientEncounterAsync(doctor, patient, checkedIn);
        return (patient, checkedIn, encounter, checkedIn.BillId
            ?? throw new InvalidOperationException("Check-in did not create a visit bill."));
    }
}
