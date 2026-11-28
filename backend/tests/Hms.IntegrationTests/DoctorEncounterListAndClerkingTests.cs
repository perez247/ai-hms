using System.Net;
using System.Net.Http.Json;
using Hms.Application.DTOs;
using Hms.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Hms.IntegrationTests;

/// <summary>Verifies the doctor's combined encounter list and the structured clerking draft/sign lifecycle.</summary>
public sealed class DoctorEncounterListAndClerkingTests(HmsApiFactory factory) : IClassFixture<HmsApiFactory>
{
    [Fact]
    public async Task Encounter_list_combines_types_and_filters_by_id_and_status()
    {
        var reception = await TestData.ClientAsync(factory, "receptionist@clinic.local");
        var doctor = await TestData.ClientAsync(factory, "doctor@clinic.local");

        var (outpatientPatient, outpatientAppointment, outpatient, _) =
            await TestData.CreateOutpatientVisitAsync(reception, doctor);
        var inpatientPatient = await TestData.CreatePatientAsync(reception);
        var (admission, inpatient) = await TestData.CreateInpatientAsync(doctor, inpatientPatient);

        var all = await doctor.GetFromJsonAsync<List<EncounterListItemDto>>("/api/clinical/encounters", TestData.Json);
        Assert.NotNull(all);
        Assert.Contains(all, x => x.Id == outpatient.Id && x.Type == EncounterType.Outpatient);
        Assert.Contains(all, x => x.Id == inpatient.Id && x.Type == EncounterType.Inpatient);

        var byId = await doctor.GetFromJsonAsync<List<EncounterListItemDto>>(
            $"/api/clinical/encounters?search={outpatient.EncounterNumber}", TestData.Json);
        Assert.NotNull(byId);
        var encounter = Assert.Single(byId);
        Assert.Equal(EncounterType.Outpatient, encounter.Type);
        Assert.Equal(outpatientPatient.PatientNumber, encounter.PatientNumber);
        Assert.Equal(outpatientAppointment.AppointmentNumber, encounter.AppointmentNumber);

        var byStatus = await doctor.GetFromJsonAsync<List<EncounterListItemDto>>(
            "/api/clinical/encounters?status=Open", TestData.Json);
        Assert.NotNull(byStatus);
        Assert.All(byStatus, x => Assert.Equal(EncounterStatus.Open, x.Status));

        var inpatientRows = await doctor.GetFromJsonAsync<List<EncounterListItemDto>>(
            "/api/clinical/encounters?type=Inpatient", TestData.Json);
        Assert.NotNull(inpatientRows);
        Assert.Contains(inpatientRows, x => x.AdmissionNumber == admission.AdmissionNumber);
    }

    [Fact]
    public async Task Structured_clerking_draft_saves_and_signing_locks_the_encounter()
    {
        var reception = await TestData.ClientAsync(factory, "receptionist@clinic.local");
        var doctor = await TestData.ClientAsync(factory, "doctor@clinic.local");
        var (_, _, encounter, _) = await TestData.CreateOutpatientVisitAsync(reception, doctor);

        var presenting = "Fever and productive cough";
        var saveDraft = await doctor.PutAsJsonAsync($"/api/clinical/encounters/{encounter.Id}/clerking",
            new SaveEncounterRequest(presenting, null, null, null, null, null, null, false), TestData.Json);
        saveDraft.EnsureSuccessStatusCode();
        var afterDraft = await saveDraft.Content.ReadFromJsonAsync<EncounterDto>(TestData.Json);
        Assert.NotNull(afterDraft);
        Assert.Equal(EncounterStatus.Open, afterDraft.Status);
        Assert.Equal(presenting, afterDraft.PresentingComplaint);

        var sign = await doctor.PutAsJsonAsync($"/api/clinical/encounters/{encounter.Id}/clerking",
            new SaveEncounterRequest(presenting, "Three days of fever and cough", null, "Mild", "R50.9", "Discharge", null, true), TestData.Json);
        sign.EnsureSuccessStatusCode();
        var afterSign = await sign.Content.ReadFromJsonAsync<EncounterDto>(TestData.Json);
        Assert.NotNull(afterSign);
        Assert.Equal(EncounterStatus.Completed, afterSign.Status);
        Assert.NotNull(afterSign.ClosedAt);
        Assert.Equal("R50.9", afterSign.Diagnosis);

        var secondSign = await doctor.PutAsJsonAsync($"/api/clinical/encounters/{encounter.Id}/clerking",
            new SaveEncounterRequest(presenting, "Three days of fever and cough", null, "Mild", "R50.9", "Discharge", null, true), TestData.Json);
        Assert.Equal(HttpStatusCode.Conflict, secondSign.StatusCode);
    }

    [Fact]
    public async Task Receptionist_cannot_write_structured_encounter_clerking()
    {
        var reception = await TestData.ClientAsync(factory, "receptionist@clinic.local");
        var doctor = await TestData.ClientAsync(factory, "doctor@clinic.local");
        var (_, _, encounter, _) = await TestData.CreateOutpatientVisitAsync(reception, doctor);

        var save = await reception.PutAsJsonAsync($"/api/clinical/encounters/{encounter.Id}/clerking",
            new SaveEncounterRequest("Fever", null, null, null, "R50.9", "Discharge", null, false), TestData.Json);
        Assert.Equal(HttpStatusCode.Forbidden, save.StatusCode);
    }

    [Fact]
    public async Task Encounter_detail_returns_prior_encounters_most_recent_first()    {
        var reception = await TestData.ClientAsync(factory, "receptionist@clinic.local");
        var doctor = await TestData.ClientAsync(factory, "doctor@clinic.local");
        var patient = await TestData.CreatePatientAsync(reception);
        var doctorId = await TestData.CurrentUserIdAsync(doctor);

        var firstAppointment = await TestData.BookAsync(reception, patient.PatientNumber, doctorId);
        var first = await TestData.CreateOutpatientEncounterAsync(doctor,
            patient, await TestData.CheckInAsync(reception, firstAppointment.Id));
        var firstSave = await doctor.PutAsJsonAsync($"/api/clinical/encounters/{first.Id}/clerking",
            new SaveEncounterRequest("First visit", null, null, null, "J00", "Discharge", null, true,
                PresentingComplaintDurationDays: 2), TestData.Json);
        firstSave.EnsureSuccessStatusCode();

        // Close the first appointment's visit bill so a second booking is allowed.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Hms.Infrastructure.Persistence.HmsDbContext>();
            var bill = await db.Bills.SingleAsync(x => x.Id == first.BillId);
            bill.Status = BillStatus.Paid;
            bill.FinanceApprovedAt = DateTimeOffset.UtcNow;
            bill.FinanceApprovedBy = first.DoctorId;
            await db.SaveChangesAsync();
        }

        var secondAppointment = await TestData.BookAsync(reception, patient.PatientNumber, doctorId);
        var second = await TestData.CreateOutpatientEncounterAsync(doctor,
            patient, await TestData.CheckInAsync(reception, secondAppointment.Id));

        var detail = await doctor.GetFromJsonAsync<EncounterDto>($"/api/clinical/encounters/{second.Id}", TestData.Json);
        Assert.NotNull(detail);
        Assert.NotNull(detail.PriorEncounters);
        var prior = Assert.Single(detail.PriorEncounters!);
        Assert.Equal(first.Id, prior.Id);
        Assert.Equal("First visit", prior.PresentingComplaint);
        Assert.Equal(2, prior.PresentingComplaintDurationDays);
        Assert.Equal("J00", prior.Diagnosis);
    }

    [Fact]
    public async Task Encounter_detail_returns_every_clerking_field_and_prescription_line()
    {
        var reception = await TestData.ClientAsync(factory, "receptionist@clinic.local");
        var doctor = await TestData.ClientAsync(factory, "doctor@clinic.local");
        var (_, _, encounter, _) = await TestData.CreateOutpatientVisitAsync(reception, doctor);
        var medicationId = await TestData.FirstMedicationIdAsync(doctor);

        var sign = await doctor.PutAsJsonAsync($"/api/clinical/encounters/{encounter.Id}/clerking",
            new SaveEncounterRequest(
                PresentingComplaint: "Cough and fever",
                HistoryOfPresentingComplaint: "Productive cough for three days",
                PastMedicalSurgicalHistory: "Asthma",
                FamilyHistory: "Hypertension",
                Diagnosis: "Acute bronchitis",
                Disposition: "FollowUp",
                Examination: [new ExaminationItemRequest("Chest/Lungs", "Crackles", false)],
                Complete: true,
                Medications:
                [
                    new PrescribeOrderRequest(medicationId, "500 mg", "Oral", "TDS", 5, 15, "Take after meals")
                ],
                PresentingComplaintDurationDays: 3,
                Clerking: new ClerkingDetailsDto(
                    RosNotes: "No weight loss",
                    Allergies: ["Penicillin"],
                    GeneralObservation: "Ill-looking",
                    PointOfCare: [new PointOfCareItemDto("RBS", "5.4 mmol/L")],
                    ClinicalImpression: "Likely viral bronchitis",
                    PlanAdvice: "Rest and fluids",
                    FollowUpDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
                    ReferralDetails: "None")), TestData.Json);
        sign.EnsureSuccessStatusCode();

        var detail = await doctor.GetFromJsonAsync<EncounterDto>($"/api/clinical/encounters/{encounter.Id}", TestData.Json);
        Assert.NotNull(detail);
        Assert.Equal(3, detail!.PresentingComplaintDurationDays);
        Assert.Equal("No weight loss", detail.Clerking?.RosNotes);
        Assert.Contains("Penicillin", detail.Clerking?.Allergies ?? []);
        Assert.Equal("Ill-looking", detail.Clerking?.GeneralObservation);
        Assert.Contains(detail.Clerking?.PointOfCare ?? [], x => x.Key == "RBS" && x.Value == "5.4 mmol/L");
        Assert.Equal("Likely viral bronchitis", detail.Clerking?.ClinicalImpression);
        Assert.Equal("Rest and fluids", detail.Clerking?.PlanAdvice);
        Assert.NotNull(detail.Clerking?.FollowUpDate);
        Assert.Equal("None", detail.Clerking?.ReferralDetails);
        Assert.Contains(detail.Examination, x => x.Name == "Chest/Lungs" && x.Result == "Crackles" && x.Normal == false);

        var prescription = Assert.Single(detail.Prescriptions);
        var line = Assert.Single(prescription.Items ?? []);
        Assert.False(string.IsNullOrWhiteSpace(line.MedicationName));
        Assert.Equal("500 mg", line.Dose);
        Assert.Equal("Oral", line.Route);
        Assert.Equal("TDS", line.Frequency);
        Assert.Equal(5, line.DurationDays);
        Assert.Equal(15m, line.QuantityPrescribed);
        Assert.Equal("Take after meals", line.Instructions);
    }

    [Fact]
    public async Task Draft_save_rejects_incomplete_plan_rows_and_persists_nothing()
    {
        var reception = await TestData.ClientAsync(factory, "receptionist@clinic.local");
        var doctor = await TestData.ClientAsync(factory, "doctor@clinic.local");
        var (_, _, encounter, _) = await TestData.CreateOutpatientVisitAsync(reception, doctor);
        var medicationId = await TestData.FirstMedicationIdAsync(doctor);

        // A started medication row with no dose must fail even though this is only a draft.
        var missingDose = await doctor.PutAsJsonAsync($"/api/clinical/encounters/{encounter.Id}/clerking",
            new SaveEncounterRequest("Fever", null, null, null, null, null, null, false,
                Medications: [new PrescribeOrderRequest(medicationId, "", "Oral", "TDS", 5, 15)]),
            TestData.Json);
        Assert.Equal(HttpStatusCode.BadRequest, missingDose.StatusCode);

        // A test row without a catalogue item is equally invalid.
        var missingTest = await doctor.PutAsJsonAsync($"/api/clinical/encounters/{encounter.Id}/clerking",
            new SaveEncounterRequest("Fever", null, null, null, null, null, null, false,
                Tests: [new ServiceOrderRequest(Guid.Empty, "Routine", null)]),
            TestData.Json);
        Assert.Equal(HttpStatusCode.BadRequest, missingTest.StatusCode);

        var detail = await doctor.GetFromJsonAsync<EncounterDto>($"/api/clinical/encounters/{encounter.Id}", TestData.Json);
        Assert.NotNull(detail);
        Assert.Empty(detail!.Prescriptions);
        Assert.Empty(detail.ServiceRequests ?? []);
    }

    [Fact]
    public async Task Draft_plan_rows_are_editable_without_duplicating_on_re_save()
    {
        var reception = await TestData.ClientAsync(factory, "receptionist@clinic.local");
        var doctor = await TestData.ClientAsync(factory, "doctor@clinic.local");
        var (_, _, encounter, _) = await TestData.CreateOutpatientVisitAsync(reception, doctor);
        var medicationId = await TestData.FirstMedicationIdAsync(doctor);
        var testId = await TestData.StockItemIdAsync(doctor, "Test", "CBC");
        var procedureId = await TestData.StockItemIdAsync(doctor, "Procedure", "APPEND");

        var first = await doctor.PutAsJsonAsync($"/api/clinical/encounters/{encounter.Id}/clerking",
            new SaveEncounterRequest("Fever", null, null, null, null, null, null, false,
                Medications: [new PrescribeOrderRequest(medicationId, "500 mg", "Oral", "TDS", 5, 15)],
                Tests: [new ServiceOrderRequest(testId, "Routine", "Rule out infection")],
                Procedures: [new ServiceOrderRequest(procedureId, "Routine", "Wound care")]),
            TestData.Json);
        first.EnsureSuccessStatusCode();
        var draft = await first.Content.ReadFromJsonAsync<EncounterDto>(TestData.Json);
        Assert.NotNull(draft);
        var item = Assert.Single(Assert.Single(draft!.Prescriptions).Items!);
        var test = Assert.Single(draft.ServiceRequests!, x => x.Kind == StockItemType.Test);
        var procedure = Assert.Single(draft.ServiceRequests!, x => x.Kind == StockItemType.Procedure);
        Assert.Equal(medicationId, item.StockItemId);
        Assert.Equal(testId, test.StockItemId);

        // Edit the dose/quantity, retitle the test and remove the procedure in the same draft.
        var second = await doctor.PutAsJsonAsync($"/api/clinical/encounters/{encounter.Id}/clerking",
            new SaveEncounterRequest("Fever", null, null, null, null, null, null, false,
                Medications: [new PrescribeOrderRequest(medicationId, "1 g", "Oral", "BD", 7, 14, Id: item.Id)],
                Tests: [new ServiceOrderRequest(testId, "Urgent", "Repeat after treatment", test.Id)],
                Procedures: []),
            TestData.Json);
        second.EnsureSuccessStatusCode();
        var edited = await second.Content.ReadFromJsonAsync<EncounterDto>(TestData.Json);
        Assert.NotNull(edited);

        var editedPrescription = Assert.Single(edited!.Prescriptions);
        var editedItem = Assert.Single(editedPrescription.Items!);
        Assert.Equal(item.Id, editedItem.Id);
        Assert.Equal("1 g", editedItem.Dose);
        Assert.Equal("BD", editedItem.Frequency);
        Assert.Equal(7, editedItem.DurationDays);
        Assert.Equal(14m, editedItem.QuantityPrescribed);

        var editedTest = Assert.Single(edited.ServiceRequests!, x => x.Kind == StockItemType.Test);
        Assert.Equal(test.Id, editedTest.Id);
        Assert.Equal("Urgent", editedTest.Priority);
        Assert.Equal("Repeat after treatment", editedTest.ClinicalNote);
        Assert.DoesNotContain(edited.ServiceRequests!, x => x.Id == procedure.Id);
    }

    [Fact]
    public async Task Removing_an_order_after_its_result_is_submitted_is_rejected()
    {
        var reception = await TestData.ClientAsync(factory, "receptionist@clinic.local");
        var doctor = await TestData.ClientAsync(factory, "doctor@clinic.local");
        var lab = await TestData.ClientAsync(factory, "lab@clinic.local");
        var (_, _, encounter, _) = await TestData.CreateOutpatientVisitAsync(reception, doctor);
        var testId = await TestData.StockItemIdAsync(doctor, "Test", "CBC");

        var draft = await doctor.PutAsJsonAsync($"/api/clinical/encounters/{encounter.Id}/clerking",
            new SaveEncounterRequest("Fever", null, null, null, null, null, null, false,
                Tests: [new ServiceOrderRequest(testId, "Routine", null)]),
            TestData.Json);
        draft.EnsureSuccessStatusCode();
        var saved = await draft.Content.ReadFromJsonAsync<EncounterDto>(TestData.Json);
        var request = Assert.Single(saved!.ServiceRequests!, x => x.Kind == StockItemType.Test);

        var submitted = await lab.PostAsJsonAsync($"/api/results/{request.Id}/submit",
            new SubmitServiceResultRequest("Haemoglobin 12 g/dL", null, null, false, null), TestData.Json);
        submitted.EnsureSuccessStatusCode();

        var removal = await doctor.PutAsJsonAsync($"/api/clinical/encounters/{encounter.Id}/clerking",
            new SaveEncounterRequest("Fever", null, null, null, null, null, null, false, Tests: []),
            TestData.Json);
        Assert.Equal(HttpStatusCode.Conflict, removal.StatusCode);
        Assert.Contains("recorded result", await removal.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Signing_without_medications_is_allowed_and_creates_no_prescription()
    {
        var reception = await TestData.ClientAsync(factory, "receptionist@clinic.local");
        var doctor = await TestData.ClientAsync(factory, "doctor@clinic.local");
        var (_, _, encounter, _) = await TestData.CreateOutpatientVisitAsync(reception, doctor);

        var sign = await doctor.PutAsJsonAsync($"/api/clinical/encounters/{encounter.Id}/clerking",
            new SaveEncounterRequest("Headache", null, null, null, "R51", "Discharge", null, true), TestData.Json);
        sign.EnsureSuccessStatusCode();
        var signed = await sign.Content.ReadFromJsonAsync<EncounterDto>(TestData.Json);
        Assert.NotNull(signed);
        Assert.Equal(EncounterStatus.Completed, signed!.Status);
        Assert.Empty(signed.Prescriptions);
    }

    [Fact]
    public async Task Outpatient_admit_disposition_creates_an_inpatient_admission()
    {
        var reception = await TestData.ClientAsync(factory, "receptionist@clinic.local");
        var doctor = await TestData.ClientAsync(factory, "doctor@clinic.local");
        var (patient, _, encounter, _) = await TestData.CreateOutpatientVisitAsync(reception, doctor);

        var sign = await doctor.PutAsJsonAsync($"/api/clinical/encounters/{encounter.Id}/clerking",
            new SaveEncounterRequest("Fever", "Three days of fever", null, null, "R50.9", "Admit", null, true), TestData.Json);
        sign.EnsureSuccessStatusCode();

        var queue = await doctor.GetFromJsonAsync<DoctorQueueDto>("/api/clinical/doctor-queue", TestData.Json);
        Assert.NotNull(queue);
        Assert.Contains(queue!.ActiveInpatients, x => x.PatientId == patient.Id);
    }

    [Fact]
    public async Task Inpatient_discharge_disposition_closes_treatment_and_starts_billing()
    {
        var reception = await TestData.ClientAsync(factory, "receptionist@clinic.local");
        var doctor = await TestData.ClientAsync(factory, "doctor@clinic.local");
        var patient = await TestData.CreatePatientAsync(reception);
        var (admission, encounter) = await TestData.CreateInpatientAsync(doctor, patient);

        var sign = await doctor.PutAsJsonAsync($"/api/clinical/encounters/{encounter.Id}/clerking",
            new SaveEncounterRequest("Fever", "Three days of fever", null, null, "R50.9", "Discharge", null, true), TestData.Json);
        sign.EnsureSuccessStatusCode();

        var refreshed = await doctor.GetFromJsonAsync<AdmissionDto>($"/api/clinical/admissions/{admission.Id}", TestData.Json);
        Assert.NotNull(refreshed);
        Assert.Equal(AdmissionStatus.PendingDischarge, refreshed!.Status);
        Assert.False(string.IsNullOrWhiteSpace(refreshed.DischargeSummary));

        var bill = await doctor.GetFromJsonAsync<BillDto>($"/api/billing/bills/{refreshed.BillId}", TestData.Json);
        Assert.NotNull(bill);
        Assert.NotEqual(BillStatus.Draft, bill!.Status);
    }

    [Fact]
    public async Task Outpatient_admit_cancels_outpatient_prescription_and_copies_to_inpatient()
    {
        var reception = await TestData.ClientAsync(factory, "receptionist@clinic.local");
        var doctor = await TestData.ClientAsync(factory, "doctor@clinic.local");
        var (patient, _, encounter, _) = await TestData.CreateOutpatientVisitAsync(reception, doctor);
        var medicationId = await TestData.FirstMedicationIdAsync(doctor);

        // Prescribe on the outpatient encounter, then sign with Admit disposition.
        await TestData.PrescribeAsync(doctor, encounter.Id, medicationId, 3);
        var sign = await doctor.PutAsJsonAsync($"/api/clinical/encounters/{encounter.Id}/clerking",
            new SaveEncounterRequest("Fever", "Three days of fever", null, null, "R50.9", "Admit", null, true), TestData.Json);
        sign.EnsureSuccessStatusCode();
        var signed = await sign.Content.ReadFromJsonAsync<EncounterDto>(TestData.Json);
        Assert.NotNull(signed);
        Assert.Equal(EncounterStatus.Completed, signed.Status);

        // The outpatient encounter's prescription should be cancelled.
        var refreshedOutpatient = await doctor.GetFromJsonAsync<EncounterDto>(
            $"/api/clinical/encounters/{encounter.Id}", TestData.Json);
        Assert.NotNull(refreshedOutpatient);
        var cancelledRx = refreshedOutpatient!.Prescriptions?.FirstOrDefault();
        Assert.NotNull(cancelledRx);
        Assert.Equal(PrescriptionStatus.Cancelled, cancelledRx.Status);

        // The patient should now be an active inpatient with a new encounter.
        var queue = await doctor.GetFromJsonAsync<DoctorQueueDto>("/api/clinical/doctor-queue", TestData.Json);
        Assert.NotNull(queue);
        var inpatientRow = Assert.Single(queue!.ActiveInpatients, x => x.PatientId == patient.Id);

        // Find the new inpatient encounter(s) for this patient.
        var allEncounters = await doctor.GetFromJsonAsync<List<EncounterListItemDto>>(
            "/api/clinical/encounters?type=Inpatient&status=Open", TestData.Json);
        var inpatientEncounters = allEncounters.Where(x => x.PatientId == patient.Id).ToList();
        Assert.NotEmpty(inpatientEncounters);

        // At least one inpatient encounter should have the copied prescription.
        var foundCopiedPrescription = false;
        foreach (var inpatient in inpatientEncounters)
        {
            var inpatientDetail = await doctor.GetFromJsonAsync<EncounterDto>(
                $"/api/clinical/encounters/{inpatient.Id}", TestData.Json);
            if (inpatientDetail?.Prescriptions?.Count > 0)
            {
                foundCopiedPrescription = true;
                Assert.Equal(medicationId.ToString(), inpatientDetail.Prescriptions!.First().Items!.First().StockItemId.ToString());
                break;
            }
        }
        Assert.True(foundCopiedPrescription, "No inpatient encounter has the copied prescription");
    }

    [Fact]
    public async Task Inpatient_discharge_commits_undispensed_prescription_to_admission_bill()
    {
        var reception = await TestData.ClientAsync(factory, "receptionist@clinic.local");
        var doctor = await TestData.ClientAsync(factory, "doctor@clinic.local");
        var finance = await TestData.ClientAsync(factory, "finance@clinic.local");
        var patient = await TestData.CreatePatientAsync(reception);
        var (admission, encounter) = await TestData.CreateInpatientAsync(doctor, patient);
        var medicationId = await TestData.FirstMedicationIdAsync(doctor);

        // Prescribe on the inpatient encounter but do NOT dispense.
        await TestData.PrescribeAsync(doctor, encounter.Id, medicationId, 3);

        // Sign with Discharge disposition.
        var sign = await doctor.PutAsJsonAsync($"/api/clinical/encounters/{encounter.Id}/clerking",
            new SaveEncounterRequest("Fever", "Three days of fever", null, null, "R50.9", "Discharge", null, true), TestData.Json);
        sign.EnsureSuccessStatusCode();

        // The admission bill should now contain medication charge lines from the undispensed prescription.
        var bill = await finance.GetFromJsonAsync<BillDto>($"/api/billing/bills/{admission.BillId}", TestData.Json);
        Assert.NotNull(bill);
        Assert.Equal(BillStatus.Committed, bill.Status);
        Assert.Contains(bill!.Lines, x => x.Category == BillLineCategory.Medication);
    }
}
