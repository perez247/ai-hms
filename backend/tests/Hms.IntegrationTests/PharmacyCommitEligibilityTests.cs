using System.Net;
using System.Net.Http.Json;
using Hms.Application.Common;
using Hms.Application.DTOs;

namespace Hms.IntegrationTests;

/// <summary>Verifies the Finance-commit gate on the pharmacy dispense path.</summary>
public sealed class PharmacyCommitEligibilityTests(HmsApiFactory factory) : IClassFixture<HmsApiFactory>
{
    [Fact]
    public async Task Outpatient_commit_is_blocked_until_the_encounter_is_signed()
    {
        var reception = await TestData.ClientAsync(factory, "receptionist@clinic.local");
        var doctor = await TestData.ClientAsync(factory, "doctor@clinic.local");
        var pharmacy = await TestData.ClientAsync(factory, "pharmacy@clinic.local");

        var (_, _, encounter, _) = await TestData.CreateOutpatientVisitAsync(reception, doctor);
        var medicationId = await TestData.FirstMedicationIdAsync(doctor);
        await TestData.PrescribeAsync(doctor, encounter.Id, medicationId, 2);

        var draft = await pharmacy.GetFromJsonAsync<PharmacyPrescriptionDto>(
            $"/api/pharmacy/encounters/{encounter.Id}/prescription", TestData.Json);
        Assert.NotNull(draft);
        Assert.False(draft!.CanCommitToFinance);
        Assert.Contains("draft", draft.CommitBlockedReason, StringComparison.OrdinalIgnoreCase);

        var blocked = await pharmacy.PostAsJsonAsync("/api/pharmacy/dispenses", new DispenseRequest(draft.Id,
            draft.Items.Select(x => new DispenseItemRequest(x.Id, x.QuantityPrescribed)).ToList()), TestData.Json);
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        Assert.Contains("draft", await blocked.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);

        await TestData.SignEncounterAsync(doctor, encounter.Id);

        var signed = await pharmacy.GetFromJsonAsync<PharmacyPrescriptionDto>(
            $"/api/pharmacy/encounters/{encounter.Id}/prescription", TestData.Json);
        Assert.NotNull(signed);
        Assert.True(signed!.CanCommitToFinance);
        Assert.Null(signed.CommitBlockedReason);

        var dispense = await pharmacy.PostAsJsonAsync("/api/pharmacy/dispenses", new DispenseRequest(signed.Id,
            signed.Items.Select(x => new DispenseItemRequest(x.Id, x.QuantityPrescribed)).ToList()), TestData.Json);
        if (!dispense.IsSuccessStatusCode)
            Assert.Fail($"{dispense.StatusCode}: {await dispense.Content.ReadAsStringAsync()}");
        var result = await dispense.Content.ReadFromJsonAsync<DispenseResultDto>(TestData.Json);
        Assert.NotNull(result);
        Assert.True(result!.CommittedToFinance);
    }

    [Fact]
    public async Task Inpatient_commit_waits_for_discharge_while_ward_dispensing_remains_available()
    {
        var reception = await TestData.ClientAsync(factory, "receptionist@clinic.local");
        var doctor = await TestData.ClientAsync(factory, "doctor@clinic.local");
        var pharmacy = await TestData.ClientAsync(factory, "pharmacy@clinic.local");

        var patient = await TestData.CreatePatientAsync(reception);
        var (_, encounter) = await TestData.CreateInpatientAsync(doctor, patient);
        var medicationId = await TestData.FirstMedicationIdAsync(doctor);
        await TestData.PrescribeAsync(doctor, encounter.Id, medicationId, 2);

        var prescription = await pharmacy.GetFromJsonAsync<PharmacyPrescriptionDto>(
            $"/api/pharmacy/encounters/{encounter.Id}/prescription", TestData.Json);
        Assert.NotNull(prescription);
        Assert.True(prescription!.IsInpatient);
        Assert.False(prescription.CanCommitToFinance);
        Assert.Contains("discharged", prescription.CommitBlockedReason, StringComparison.OrdinalIgnoreCase);

        // Ward dispensing stays allowed; the charge waits on the draft admission bill.
        var dispense = await pharmacy.PostAsJsonAsync("/api/pharmacy/dispenses", new DispenseRequest(prescription.Id,
            prescription.Items.Select(x => new DispenseItemRequest(x.Id, x.QuantityPrescribed)).ToList()), TestData.Json);
        if (!dispense.IsSuccessStatusCode)
            Assert.Fail($"{dispense.StatusCode}: {await dispense.Content.ReadAsStringAsync()}");
        var result = await dispense.Content.ReadFromJsonAsync<DispenseResultDto>(TestData.Json);
        Assert.NotNull(result);
        Assert.False(result!.CommittedToFinance);
    }
}
