using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Domain.Entities;
using Hms.Domain.Enums;

namespace Hms.Application.Services;

public interface IClinicalService
{
    Task<EncounterDto> CreateOutpatientEncounterAsync(CreateOutpatientEncounterRequest request, CancellationToken cancellationToken = default);
    Task<AdmissionDto> CreateAdmissionAsync(CreateAdmissionRequest request, CancellationToken cancellationToken = default);
    Task<EncounterDto> AddInpatientEncounterAsync(CreateInpatientEncounterRequest request, CancellationToken cancellationToken = default);
    Task<VitalDto> RecordVitalsAsync(RecordVitalsRequest request, CancellationToken cancellationToken = default);
    Task<EncounterDto> GetEncounterAsync(Guid encounterId, CancellationToken cancellationToken = default);
    Task<PagedResult<ServiceRequestSummaryDto>> GetEncounterResultsAsync(Guid encounterId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<AdmissionDto> GetAdmissionAsync(Guid admissionId, CancellationToken cancellationToken = default);
    Task<PrescriptionSummaryDto> CreatePrescriptionAsync(CreatePrescriptionRequest request, CancellationToken cancellationToken = default);
    Task<InvestigationSummaryDto> CreateInvestigationAsync(CreateInvestigationRequest request, CancellationToken cancellationToken = default);
    Task<InvestigationSummaryDto> CompleteInvestigationAsync(Guid investigationId, CompleteInvestigationRequest request, CancellationToken cancellationToken = default);
    Task<SurgerySummaryDto> CreateSurgeryAsync(CreateSurgeryRequest request, CancellationToken cancellationToken = default);
    Task<SurgerySummaryDto> CompleteSurgeryAsync(Guid surgeryId, CompleteSurgeryRequest request, CancellationToken cancellationToken = default);
    Task<Guid> RecordAdministrationAsync(RecordAdministrationRequest request, CancellationToken cancellationToken = default);
    Task<DoctorQueueDto> GetDoctorQueueAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EncounterListItemDto>> SearchEncountersAsync(EncounterType? type, EncounterStatus? status,
        DateOnly? date, string? search, CancellationToken cancellationToken = default);
    Task<EncounterDto> SaveEncounterAsync(Guid encounterId, SaveEncounterRequest request, CancellationToken cancellationToken = default);
    Task<DischargeRequestResult> RequestDischargeAsync(RequestDischargeRequest request, CancellationToken cancellationToken = default);
    Task<AdmissionDto> CompleteDischargeAsync(Guid admissionId, CancellationToken cancellationToken = default);
}
