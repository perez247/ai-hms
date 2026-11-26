using System.Net;
using System.Net.Http.Json;
using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Domain.Enums;

namespace Hms.IntegrationTests;

/// <summary>Covers doctor queues, unlinked fitness/filters, HMO tariffs and tariff-driven pricing.</summary>
public sealed class NewWorkflowFeaturesTests(HmsApiFactory factory) : IClassFixture<HmsApiFactory>
{
    [Fact]
    public async Task Doctor_queue_lists_checked_in_patients_and_clears_after_encounter()
    {
        var reception = await TestData.ClientAsync(factory, "receptionist@clinic.local");
        var doctor = await TestData.ClientAsync(factory, "doctor@clinic.local");

        var patient = await TestData.CreatePatientAsync(reception);
        var doctorId = await TestData.CurrentUserIdAsync(doctor);
        var appointment = await TestData.BookAsync(reception, patient.PatientNumber, doctorId);
        var created = await TestData.CheckInAsync(reception, appointment.Id);

        var admissionPatient = await TestData.CreatePatientAsync(reception);
        var (admission, _) = await TestData.CreateInpatientAsync(doctor, admissionPatient);

        var queue = await doctor.GetFromJsonAsync<DoctorQueueDto>("/api/clinical/doctor-queue", TestData.Json);
        Assert.NotNull(queue);
        Assert.Contains(queue.AwaitingOutpatients, x => x.SourceId == created.Id && x.PatientNumber == patient.PatientNumber);
        Assert.Contains(queue.ActiveInpatients, x => x.ReferenceNumber == admission.AdmissionNumber);

        var encounter = await doctor.PostAsJsonAsync("/api/clinical/encounters/outpatient", new CreateOutpatientEncounterRequest(
            patient.Id, created.DoctorId, created.Id, false, null, "Queue test encounter"), TestData.Json);
        encounter.EnsureSuccessStatusCode();

        var after = await doctor.GetFromJsonAsync<DoctorQueueDto>("/api/clinical/doctor-queue", TestData.Json);
        Assert.NotNull(after);
        Assert.DoesNotContain(after.AwaitingOutpatients, x => x.SourceId == created.Id);
    }

    [Fact]
    public async Task Fitness_allows_unlinked_creation_and_combined_filters()
    {
        var reception = await TestData.ClientAsync(factory, "receptionist@clinic.local");
        var nurse = await TestData.ClientAsync(factory, "nurse@clinic.local");
        var doctor = await TestData.ClientAsync(factory, "doctor@clinic.local");
        var finance = await TestData.ClientAsync(factory, "finance@clinic.local");
        var reference = $"FREE-{Guid.NewGuid():N}"[..20].ToUpperInvariant();

        var create = await reception.PostAsJsonAsync("/api/fitness", new CreateFitnessCertificateRequest(
            reference, "Unlinked Person", 33, Sex.Other, "+234-000-queue",
            FitnessPaymentType.Private, null, null, DateOnly.FromDateTime(DateTime.UtcNow), null), TestData.Json);
        create.EnsureSuccessStatusCode();
        var record = await create.Content.ReadFromJsonAsync<FitnessCertificateDto>(TestData.Json);
        Assert.NotNull(record);
        Assert.Null(record.PatientId);
        Assert.Equal(reference, record.PatientNumber);

        var clinical = await nurse.PostAsJsonAsync($"/api/fitness/{record.Id}/clinical", new UpdateFitnessClinicalInfoRequest(
            [new("BP", "120/80")]), TestData.Json);
        clinical.EnsureSuccessStatusCode();
        (await nurse.PostAsync($"/api/fitness/{record.Id}/submit", null)).EnsureSuccessStatusCode();
        (await doctor.PostAsJsonAsync($"/api/fitness/{record.Id}/decision", new FitnessDecisionRequest(true, null), TestData.Json)).EnsureSuccessStatusCode();
        (await finance.PostAsJsonAsync($"/api/fitness/{record.Id}/payment", new FitnessPaymentRequest(true), TestData.Json)).EnsureSuccessStatusCode();

        var filtered = await finance.GetFromJsonAsync<PagedResult<FitnessCertificateDto>>(
            $"/api/fitness?status=Approved&paymentType=Private&paymentStatus=Paid&search={reference}", TestData.Json);
        Assert.NotNull(filtered);
        Assert.Contains(filtered.Items, x => x.Id == record.Id);

        var noMatch = await finance.GetFromJsonAsync<PagedResult<FitnessCertificateDto>>(
            $"/api/fitness?status=AwaitingNurse&paymentType=Private&paymentStatus=Paid&search={reference}", TestData.Json);
        Assert.NotNull(noMatch);
        Assert.Empty(noMatch.Items);
    }

    [Fact]
    public async Task Hmo_officer_registers_tariff_and_finance_applies_negotiated_price()
    {
        var reception = await TestData.ClientAsync(factory, "receptionist@clinic.local");
        var hmoOfficer = await TestData.ClientAsync(factory, "hmo@clinic.local");
        var doctor = await TestData.ClientAsync(factory, "doctor@clinic.local");
        var finance = await TestData.ClientAsync(factory, "finance@clinic.local");
        var pharmacist = await TestData.ClientAsync(factory, "pharmacy@clinic.local");

        var forbidden = await doctor.PostAsJsonAsync("/api/hmo/companies", new
        {
            code = "NOPE", name = "Should fail", contactPerson = "", email = "", phone = "", address = "",
            contractTerms = "", initialTariff = (object?)null
        }, TestData.Json);
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
        }, TestData.Json);
        registration.EnsureSuccessStatusCode();
        var directory = await hmoOfficer.GetFromJsonAsync<HmoDirectoryEntryDto[]>("api/hmo/companies", TestData.Json);
        var hmo = Assert.Single(directory!, x => x.Code == code);
        var tariff = Assert.Single(hmo.Tariffs);
        Assert.Equal(TariffStatus.Active, tariff.Status);
        Assert.Equal(100m, Assert.Single(tariff.Items).UnitPrice);

        var patient = await TestData.CreatePatientAsync(reception);
        var bill = await finance.PostAsJsonAsync("/api/billing/bills", new CreateManualBillRequest(
            patient.Id, hmo.Id, null, "Tariff pricing test",
            [
                new CreateManualBillLineRequest(BillLineCategory.Medication, "Test drug", 2m, 500m, "TESTDRUG"),
                new CreateManualBillLineRequest(BillLineCategory.Medication, "Unmatched drug", 1m, 250m, "UNKNOWN-CODE")
            ]), TestData.Json);
        bill.EnsureSuccessStatusCode();
        var result = await bill.Content.ReadFromJsonAsync<ManualBillResultDto>(TestData.Json);
        Assert.NotNull(result);
        var priced = Assert.Single(result.Bill.Lines, x => x.Description == "Test drug");
        Assert.Equal(100m, priced.UnitPrice);
        Assert.Equal(100m, priced.HmoAmount);
        Assert.Equal(100m, priced.PatientAmount);
        Assert.Contains(result.Warnings, x => x.Contains("UNKNOWN-CODE"));

        // A visit for a patient enrolled in that HMO offers both private and negotiated price options.
        var (_, _, encounter, _) = await TestData.CreateOutpatientVisitAsync(reception, doctor, hmo.Id);
        var medicationId = await TestData.FirstMedicationIdAsync(doctor);
        await TestData.PrescribeAsync(doctor, encounter.Id, medicationId);
        var options = await pharmacist.GetFromJsonAsync<PrescriptionPriceOptionsDto>(
            $"/api/pharmacy/encounters/{encounter.Id}/price-options", TestData.Json);
        Assert.NotNull(options);
        var privateOption = Assert.Single(options.Options, x => x.PayerType == "Private");
        var hmoOption = Assert.Single(options.Options, x => x.HmoCompanyId == hmo.Id);
        Assert.True(privateOption.TotalPrice > 0);
        Assert.Contains(hmoOption.Warnings, x => x.Contains("No active tariff"));
    }
}
