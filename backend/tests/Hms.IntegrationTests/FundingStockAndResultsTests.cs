using System.Net;
using System.Net.Http.Json;
using Hms.Application.DTOs;
using Hms.Domain.Enums;

namespace Hms.IntegrationTests;

/// <summary>End-to-end coverage for funding verification, Stocks ordering and post-sign result editing.</summary>
public sealed class FundingStockAndResultsTests(HmsApiFactory factory) : IClassFixture<HmsApiFactory>
{
    [Fact]
    public async Task Receptionist_can_book_with_verified_hmo_funding_but_not_arbitrary_enrollment()
    {
        var reception = await TestData.ClientAsync(factory, "receptionist@clinic.local");
        var doctor = await TestData.ClientAsync(factory, "doctor@clinic.local");

        var hmoId = await TestData.FirstHmoIdAsync(reception);
        var patient = await TestData.CreatePatientAsync(reception, hmoId);
        var enrollments = await doctor.GetFromJsonAsync<List<HmoEnrollmentOptionDto>>(
            $"/api/reference/patients/{patient.Id}/hmo-enrollments", TestData.Json);
        Assert.NotNull(enrollments);
        var enrollment = Assert.Single(enrollments!);

        var doctors = await doctor.GetFromJsonAsync<List<DoctorOptionDto>>("/api/reference/doctors", TestData.Json);
        Assert.NotNull(doctors);
        var doctorId = Assert.Single(doctors!).Id;

        var booked = await reception.PostAsJsonAsync("/api/appointments", new CreateAppointmentRequest(
            patient.PatientNumber, doctorId, DateTimeOffset.UtcNow.AddDays(2), 30, AppointmentReasons.Consultation,
            FundingSource.Hmo, enrollment.Id), TestData.Json);
        booked.EnsureSuccessStatusCode();
        var appointment = await booked.Content.ReadFromJsonAsync<AppointmentDto>(TestData.Json);
        Assert.NotNull(appointment);
        Assert.Equal(FundingSource.Hmo, appointment!.FundingSource);
        Assert.Equal(enrollment.Id, appointment.HmoEnrollmentId);

        var ongoing = await reception.GetFromJsonAsync<OngoingAppointmentDto>(
            $"/api/appointments/ongoing?patientNumber={patient.PatientNumber}", TestData.Json);
        Assert.NotNull(ongoing);
        Assert.Equal(appointment.Id, ongoing!.Id);

        // A second booking is blocked while the first appointment's visit bill is still open.
        var blocked = await reception.PostAsJsonAsync("/api/appointments", new CreateAppointmentRequest(
            patient.PatientNumber, doctorId, DateTimeOffset.UtcNow.AddDays(3), 30, AppointmentReasons.Consultation,
            FundingSource.Private, null), TestData.Json);
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        var blockedBody = await blocked.Content.ReadAsStringAsync();
        Assert.Contains("ongoing appointment", blockedBody, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(appointment.AppointmentNumber, blockedBody);

        var otherPatient = await TestData.CreatePatientAsync(reception);
        var invalid = await reception.PostAsJsonAsync("/api/appointments", new CreateAppointmentRequest(
            otherPatient.PatientNumber, doctorId, DateTimeOffset.UtcNow.AddDays(3), 30, AppointmentReasons.Consultation,
            FundingSource.Hmo, Guid.NewGuid()), TestData.Json);
        Assert.Equal(HttpStatusCode.Conflict, invalid.StatusCode);
    }

    [Fact]
    public async Task Doctor_orders_tests_and_procedures_and_lab_edits_result_after_signing()
    {
        var reception = await TestData.ClientAsync(factory, "receptionist@clinic.local");
        var doctor = await TestData.ClientAsync(factory, "doctor@clinic.local");
        var lab = await TestData.ClientAsync(factory, "lab@clinic.local");

        var (_, _, encounter, _) = await TestData.CreateOutpatientVisitAsync(reception, doctor);
        var tests = await doctor.GetFromJsonAsync<List<StockItemDto>>("/api/reference/stock-items?type=Test", TestData.Json);
        var procedures = await doctor.GetFromJsonAsync<List<StockItemDto>>("/api/reference/stock-items?type=Procedure", TestData.Json);
        Assert.NotNull(tests);
        Assert.NotNull(procedures);
        var test = tests!.First();
        var procedure = procedures!.First();

        var draft = await doctor.PutAsJsonAsync($"/api/clinical/encounters/{encounter.Id}/clerking",
            new SaveEncounterRequest("Fever", "Three days", null, null, "R50.9", "Discharge", null, false,
                null,
                [new ServiceOrderRequest(test.Id, "Routine", "Rule out infection")],
                [new ServiceOrderRequest(procedure.Id, "Routine", "Assessment")]), TestData.Json);
        draft.EnsureSuccessStatusCode();

        var sign = await doctor.PutAsJsonAsync($"/api/clinical/encounters/{encounter.Id}/clerking",
            new SaveEncounterRequest("Fever", "Three days", null, null, "R50.9", "Discharge", null, true), TestData.Json);
        sign.EnsureSuccessStatusCode();
        var signed = await sign.Content.ReadFromJsonAsync<EncounterDto>(TestData.Json);
        Assert.NotNull(signed);
        Assert.Equal(EncounterStatus.Completed, signed!.Status);
        Assert.Equal(2, signed.ServiceRequests.Count);
        Assert.All(signed.ServiceRequests, x => Assert.Equal(ServiceResultStatus.Pending, x.ResultStatus));

        var requests = await lab.GetFromJsonAsync<List<ServiceRequestDto>>("/api/results/tests", TestData.Json);
        Assert.NotNull(requests);
        var testRequest = Assert.Single(requests!.Where(x => x.EncounterId == encounter.Id));
        Assert.Equal(encounter.PatientNumber, testRequest.PatientNumber);

        var saved = await lab.PutAsJsonAsync($"/api/results/{testRequest.Id}",
            new SaveServiceResultRequest("Haemoglobin 12 g/dL", null, null, false, "Initial entry"), TestData.Json);
        saved.EnsureSuccessStatusCode();

        // Doctors may enter lab result values, not only read the queue.
        var doctorDraft = await doctor.PutAsJsonAsync($"/api/results/{testRequest.Id}",
            new SaveServiceResultRequest("Haemoglobin 12 g/dL", "Checked by doctor", null, false, "Doctor review"), TestData.Json);
        doctorDraft.EnsureSuccessStatusCode();

        var submitted = await lab.PostAsJsonAsync($"/api/results/{testRequest.Id}/submit",
            new SubmitServiceResultRequest("Haemoglobin 12 g/dL — within range", null, null, false, null), TestData.Json);
        submitted.EnsureSuccessStatusCode();
        var afterSubmit = await submitted.Content.ReadFromJsonAsync<ServiceRequestDto>(TestData.Json);
        Assert.NotNull(afterSubmit);
        Assert.Equal(ServiceResultStatus.Submitted, afterSubmit!.ResultStatus);
        Assert.Equal(ServiceRequestStatus.Completed, afterSubmit.Status);

        // Returned results are visible on the encounter detail page for the doctor.
        var detail = await doctor.GetFromJsonAsync<EncounterDto>($"/api/clinical/encounters/{encounter.Id}", TestData.Json);
        Assert.NotNull(detail);
        var returned = Assert.Single(detail!.ServiceRequests, x => x.Id == testRequest.Id);
        Assert.Equal("Haemoglobin 12 g/dL — within range", returned.ResultText);
        Assert.Equal(ServiceResultStatus.Submitted, returned.ResultStatus);

        // Submitted results are locked; neither a draft save nor a resubmission is accepted.
        var amended = await lab.PutAsJsonAsync($"/api/results/{testRequest.Id}",
            new SaveServiceResultRequest("Haemoglobin 11.8 g/dL — corrected", null, null, true, "Corrected value"), TestData.Json);
        Assert.Equal(HttpStatusCode.Conflict, amended.StatusCode);
        Assert.Contains("locked", await amended.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);

        var resubmitted = await lab.PostAsJsonAsync($"/api/results/{testRequest.Id}/submit",
            new SubmitServiceResultRequest("Haemoglobin 11.8 g/dL — corrected", null, null, true, null), TestData.Json);
        Assert.Equal(HttpStatusCode.Conflict, resubmitted.StatusCode);

        var afterSignAttempt = await doctor.PutAsJsonAsync($"/api/clinical/encounters/{encounter.Id}/clerking",
            new SaveEncounterRequest("Fever", null, null, null, "R50.9", "Discharge", null, true), TestData.Json);
        Assert.Equal(HttpStatusCode.Conflict, afterSignAttempt.StatusCode);
    }

    [Fact]
    public async Task Medication_only_stock_is_enforced_and_hmo_prices_are_seeded()
    {
        var doctor = await TestData.ClientAsync(factory, "doctor@clinic.local");
        var pharmacy = await TestData.ClientAsync(factory, "pharmacy@clinic.local");
        var denied = await doctor.GetAsync("/api/stocks");
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        var items = await pharmacy.GetFromJsonAsync<List<StockItemDto>>("/api/stocks", TestData.Json);
        Assert.NotNull(items);
        Assert.Contains(items!, x => x.ItemType == StockItemType.Medication && x.Quantity >= 0);
        Assert.All(items!.Where(x => x.ItemType != StockItemType.Medication), x => Assert.Equal(0m, x.Quantity));
        Assert.Contains(items!, x => x.Prices.Any(p => p.FundingSource == FundingSource.Private));
        Assert.Contains(items!, x => x.Prices.Any(p => p.FundingSource == FundingSource.Hmo));
    }
}
