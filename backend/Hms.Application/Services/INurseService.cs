using Hms.Application.DTOs;

namespace Hms.Application.Services;

/// <summary>Nurse-facing patient search follow-up: patient-level vitals, encounter services and shift handover.</summary>
public interface INurseService
{
    Task<PatientVitalsPageDto> GetVitalsAsync(string patientNumber, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<VitalDto> RecordPatientVitalsAsync(RecordPatientVitalsRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NurseAppointmentDto>> GetScheduledAppointmentsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NurseInpatientDto>> GetInpatientsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NurseEncounterDto>> GetOpenEncountersAsync(string patientNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShiftHandoverDto>> GetHandoversAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ShiftHandoverDto> CreateHandoverAsync(CreateShiftHandoverRequest request, CancellationToken cancellationToken = default);
}
