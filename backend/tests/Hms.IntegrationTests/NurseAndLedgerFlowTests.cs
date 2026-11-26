using System.Net.Http.Json;
using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Domain.Enums;

namespace Hms.IntegrationTests;

/// <summary>Covers nurse patient search/vitals, encounter administration billing and ledger debits.</summary>
public sealed class NurseAndLedgerFlowTests(HmsApiFactory factory) : IClassFixture<HmsApiFactory>
{
    [Fact]
    public async Task Nurse_can_search_patient_and_record_patient_level_vitals()
    {
        var reception = await TestData.ClientAsync(factory, "receptionist@clinic.local");
        var nurse = await TestData.ClientAsync(factory, "nurse@clinic.local");
        var patient = await TestData.CreatePatientAsync(reception, firstName: "NurseSearch");

        var search = await nurse.GetFromJsonAsync<PagedResult<PatientSearchItem>>(
            $"/api/patients?search={patient.PatientNumber}", TestData.Json);
        Assert.NotNull(search);
        Assert.Contains(search.Items, x => x.PatientNumber == patient.PatientNumber);

        var request = new RecordPatientVitalsRequest(patient.PatientNumber, 36.8m, 120, 80, 72, 16, 99, 68m, 165m, 1, "Nurse station");
        var record = await nurse.PostAsJsonAsync($"/api/nurse/patients/{patient.PatientNumber}/vitals", request, TestData.Json);
        record.EnsureSuccessStatusCode();

        var page = await nurse.GetFromJsonAsync<PatientVitalsPageDto>(
            $"/api/nurse/patients/{patient.PatientNumber}/vitals", TestData.Json);
        Assert.NotNull(page);
        Assert.True(page.Total >= 1);
    }

    [Fact]
    public async Task Encounter_detail_loads_vitals_from_the_patient_not_the_encounter()
    {
        var reception = await TestData.ClientAsync(factory, "receptionist@clinic.local");
        var nurse = await TestData.ClientAsync(factory, "nurse@clinic.local");
        var doctor = await TestData.ClientAsync(factory, "doctor@clinic.local");
        var (patient, _, encounter, _) = await TestData.CreateOutpatientVisitAsync(reception, doctor);

        var request = new RecordPatientVitalsRequest(patient.PatientNumber, 37.2m, 118, 76, 78, 16, 98, 70m, 170m, 2, "Triage");
        var record = await nurse.PostAsJsonAsync($"/api/nurse/patients/{patient.PatientNumber}/vitals", request, TestData.Json);
        record.EnsureSuccessStatusCode();

        var detail = await doctor.GetFromJsonAsync<EncounterDto>(
            $"/api/clinical/encounters/{encounter.Id}", TestData.Json);
        Assert.NotNull(detail);
        Assert.Contains(detail!.Vitals, x => x.TemperatureC == 37.2m && x.Notes == "Triage");
    }

    [Fact]
    public async Task Encounter_detail_returns_only_the_five_most_recent_vitals_newest_first()
    {
        var reception = await TestData.ClientAsync(factory, "receptionist@clinic.local");
        var nurse = await TestData.ClientAsync(factory, "nurse@clinic.local");
        var doctor = await TestData.ClientAsync(factory, "doctor@clinic.local");
        var (patient, _, encounter, _) = await TestData.CreateOutpatientVisitAsync(reception, doctor);

        for (var i = 0; i < 7; i++)
        {
            var request = new RecordPatientVitalsRequest(patient.PatientNumber,
                36.5m + i / 10m, 120, 80, 70 + i, 16, 98, 70m, 170m, 1, $"Reading {i}");
            var record = await nurse.PostAsJsonAsync(
                $"/api/nurse/patients/{patient.PatientNumber}/vitals", request, TestData.Json);
            record.EnsureSuccessStatusCode();
        }

        var detail = await doctor.GetFromJsonAsync<EncounterDto>(
            $"/api/clinical/encounters/{encounter.Id}", TestData.Json);
        Assert.NotNull(detail);
        Assert.Equal(5, detail!.Vitals.Count);
        Assert.DoesNotContain(detail.Vitals, x => x.Notes == "Reading 0");
        Assert.DoesNotContain(detail.Vitals, x => x.Notes == "Reading 1");
        Assert.Contains(detail.Vitals, x => x.Notes == "Reading 6");
        for (var i = 1; i < detail.Vitals.Count; i++)
            Assert.True(detail.Vitals[i - 1].RecordedAt >= detail.Vitals[i].RecordedAt);
    }

    [Fact]
    public async Task Administered_inpatient_prescription_creates_a_billing_line()
    {
        var reception = await TestData.ClientAsync(factory, "receptionist@clinic.local");
        var doctor = await TestData.ClientAsync(factory, "doctor@clinic.local");
        var nurse = await TestData.ClientAsync(factory, "nurse@clinic.local");
        var pharmacy = await TestData.ClientAsync(factory, "pharmacy@clinic.local");
        var finance = await TestData.ClientAsync(factory, "finance@clinic.local");

        var patient = await TestData.CreatePatientAsync(reception);
        var (_, encounter) = await TestData.CreateInpatientAsync(doctor, patient);
        var medicationId = await TestData.FirstMedicationIdAsync(doctor);
        await TestData.PrescribeAsync(doctor, encounter.Id, medicationId, 3);

        var encounters = await nurse.GetFromJsonAsync<List<NurseEncounterDto>>(
            $"/api/nurse/patients/{patient.PatientNumber}/encounters", TestData.Json);
        Assert.Contains(encounters!, x => x.Id == encounter.Id);
        var prescription = await pharmacy.GetFromJsonAsync<PharmacyPrescriptionDto>(
            $"/api/pharmacy/encounters/{encounter.Id}/prescription", TestData.Json);
        Assert.NotNull(prescription);

        var dispense = await pharmacy.PostAsJsonAsync("/api/pharmacy/dispenses", new DispenseRequest(prescription!.Id,
            prescription.Items.Select(x => new DispenseItemRequest(x.Id, x.QuantityPrescribed)).ToList()), TestData.Json);
        dispense.EnsureSuccessStatusCode();

        var refreshed = await nurse.GetFromJsonAsync<List<NurseEncounterDto>>(
            $"/api/nurse/patients/{patient.PatientNumber}/encounters", TestData.Json);
        var item = refreshed!.SelectMany(x => x.Prescriptions).Single(x => x.CanAdminister);
        var administer = await nurse.PostAsJsonAsync("/api/clinical/administrations", new RecordAdministrationRequest(
            item.DispenseItemId!.Value, 1m, item.Route, null, "Nurse test"), TestData.Json);
        administer.EnsureSuccessStatusCode();

        var bills = await finance.GetFromJsonAsync<PagedResult<BillDto>>(
            $"/api/billing/bills?patientId={patient.Id}", TestData.Json);
        Assert.Contains(bills!.Items.SelectMany(x => x.Lines),
            line => line.Category == BillLineCategory.Medication);
    }

    [Fact]
    public async Task Handover_endpoint_is_nurse_only_and_allows_page_size_100()
    {
        var nurse = await TestData.ClientAsync(factory, "nurse@clinic.local");
        var doctor = await TestData.ClientAsync(factory, "doctor@clinic.local");

        var allowed = await nurse.GetAsync("/api/nurse/handovers?page=1&pageSize=100");
        Assert.Equal(System.Net.HttpStatusCode.OK, allowed.StatusCode);
        var page = await allowed.Content.ReadFromJsonAsync<List<ShiftHandoverDto>>(TestData.Json);
        Assert.NotNull(page);

        var denied = await doctor.GetAsync("/api/nurse/handovers?page=1&pageSize=100");
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, denied.StatusCode);
    }

    [Fact]
    public async Task Triage_lists_pending_appointments_and_active_inpatients()
    {
        var nurse = await TestData.ClientAsync(factory, "nurse@clinic.local");
        var appointments = await nurse.GetAsync("/api/nurse/appointments");
        Assert.Equal(System.Net.HttpStatusCode.OK, appointments.StatusCode);
        var inpatients = await nurse.GetAsync("/api/nurse/inpatients");
        Assert.Equal(System.Net.HttpStatusCode.OK, inpatients.StatusCode);
    }

    [Fact]
    public async Task Paid_appointment_leaves_the_nurse_vitals_queue()
    {
        var reception = await TestData.ClientAsync(factory, "receptionist@clinic.local");
        var doctor = await TestData.ClientAsync(factory, "doctor@clinic.local");
        var nurse = await TestData.ClientAsync(factory, "nurse@clinic.local");
        var pharmacy = await TestData.ClientAsync(factory, "pharmacy@clinic.local");
        var finance = await TestData.ClientAsync(factory, "finance@clinic.local");

        var patient = await TestData.CreatePatientAsync(reception);
        var doctorId = await TestData.CurrentUserIdAsync(doctor);
        var appointment = await TestData.BookAsync(reception, patient.PatientNumber, doctorId);
        var checkedIn = await TestData.CheckInAsync(reception, appointment.Id);
        var billId = checkedIn.BillId ?? throw new InvalidOperationException("Check-in did not create a visit bill.");

        var queue = await nurse.GetFromJsonAsync<List<NurseAppointmentDto>>("/api/nurse/appointments", TestData.Json);
        Assert.NotNull(queue);
        Assert.Contains(queue!, x => x.Id == checkedIn.Id);

        // Commit the visit bill through the normal dispensing flow.
        var encounter = await TestData.CreateOutpatientEncounterAsync(doctor, patient, checkedIn);
        var medicationId = await TestData.FirstMedicationIdAsync(doctor);
        await TestData.PrescribeAsync(doctor, encounter.Id, medicationId, 3);
        await TestData.SignEncounterAsync(doctor, encounter.Id);
        var prescription = await pharmacy.GetFromJsonAsync<PharmacyPrescriptionDto>(
            $"/api/pharmacy/encounters/{encounter.Id}/prescription", TestData.Json);
        Assert.NotNull(prescription);
        var dispense = await pharmacy.PostAsJsonAsync("/api/pharmacy/dispenses", new DispenseRequest(
            prescription!.Id,
            prescription.Items.Select(x => new DispenseItemRequest(x.Id, x.QuantityPrescribed)).ToList()), TestData.Json);
        dispense.EnsureSuccessStatusCode();

        // A committed but unpaid bill keeps the appointment in the queue.
        var committedQueue = await nurse.GetFromJsonAsync<List<NurseAppointmentDto>>("/api/nurse/appointments", TestData.Json);
        Assert.NotNull(committedQueue);
        Assert.Contains(committedQueue!, x => x.Id == checkedIn.Id);

        var bill = await finance.GetFromJsonAsync<BillDto>($"/api/billing/bills/{billId}", TestData.Json);
        Assert.NotNull(bill);
        var payment = await finance.PostAsJsonAsync($"/api/billing/bills/{billId}/payments", new PaymentRequest(
            bill!.Balance, PaymentMethod.Cash, "NURSE-QUEUE", Guid.NewGuid().ToString(), null), TestData.Json);
        payment.EnsureSuccessStatusCode();

        var paidQueue = await nurse.GetFromJsonAsync<List<NurseAppointmentDto>>("/api/nurse/appointments", TestData.Json);
        Assert.NotNull(paidQueue);
        Assert.DoesNotContain(paidQueue!, x => x.Id == checkedIn.Id);
    }

    [Fact]
    public async Task Purchase_posts_a_debit_to_the_ledger_with_reference()
    {
        var finance = await TestData.ClientAsync(factory, "finance@clinic.local");
        var response = await finance.PostAsJsonAsync("/api/billing/account-book/purchases",
            new AccountBookEntryRequest(Hms.Domain.Enums.AccountEntryType.Expense, "Supplies", 250m,
                "Examination gloves", DateTimeOffset.UtcNow, "INV-LEDGER-1"), TestData.Json);
        response.EnsureSuccessStatusCode();
        var ledger = await finance.GetFromJsonAsync<PagedResult<AccountBookEntryDto>>(
            "/api/billing/account-book?pageSize=100", TestData.Json);
        Assert.Contains(ledger!.Items, x => x.Direction == "Debit" && x.Reference == "INV-LEDGER-1");
    }
}
