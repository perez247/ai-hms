using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hms.Application.DTOs;
using Hms.Domain.Enums;

namespace Hms.IntegrationTests;

/// <summary>Receptionist-editable basic patient demographics including the new registration fields.</summary>
public sealed class PatientDemographicsTests(HmsApiFactory factory) : IClassFixture<HmsApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task Receptionist_updates_basic_patient_demographics_with_new_fields()
    {
        var reception = await TestData.ClientAsync(factory, "receptionist@clinic.local");
        var doctor = await TestData.ClientAsync(factory, "doctor@clinic.local");

        var registered = await TestData.CreatePatientAsync(reception);
        var before = await doctor.GetFromJsonAsync<PatientBasicProfileDto>(
            $"/api/patients/number/{registered.PatientNumber}", Json);
        Assert.NotNull(before);
        var patient = before!.Patient;
        Assert.NotNull(patient);

        var update = await reception.PutAsJsonAsync($"/api/patients/{patient.Id}", new UpdatePatientRequest(
            "Ada", null, "Okafor", Sex.Female, patient.DateOfBirth, patient.Phone, patient.Email, patient.Address,
            "Lagos State", "Badagry", "Penicillin, sulfa", "Nursing",
            patient.NextOfKinName, patient.NextOfKinPhone), Json);
        update.EnsureSuccessStatusCode();
        var after = await doctor.GetFromJsonAsync<PatientBasicProfileDto>(
            $"/api/patients/number/{registered.PatientNumber}", Json);
        Assert.NotNull(after);
        Assert.Equal("Lagos State", after!.Patient.StateOfOrigin);
        Assert.Equal("Badagry", after.Patient.HomeTown);
        Assert.Equal("Penicillin, sulfa", after.Patient.Allergy);
        Assert.Equal("Nursing", after.Patient.Occupation);
    }
}
