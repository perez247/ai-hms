using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Patients;

public sealed record GetPatientMedicationsByNumberQuery(string PatientNumber) : IRequest<IReadOnlyList<PatientMedicationDto>>;

public sealed class GetPatientMedicationsByNumberQueryHandler(IPatientProfileService profiles)
    : IRequestHandler<GetPatientMedicationsByNumberQuery, IReadOnlyList<PatientMedicationDto>>
{
    public Task<IReadOnlyList<PatientMedicationDto>> Handle(GetPatientMedicationsByNumberQuery request, CancellationToken cancellationToken) =>
        profiles.GetMedicationsByNumberAsync(request.PatientNumber, cancellationToken);
}

public sealed class GetPatientMedicationsByNumberQueryValidator : AbstractValidator<GetPatientMedicationsByNumberQuery>
{
    public GetPatientMedicationsByNumberQueryValidator() => RuleFor(x => x.PatientNumber).NotEmpty().MaximumLength(50);
}
