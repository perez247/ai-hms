using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Patients;

public sealed record GetPatientEncountersByNumberQuery(string PatientNumber) : IRequest<IReadOnlyList<PatientEncounterSummaryDto>>;

public sealed class GetPatientEncountersByNumberQueryHandler(IPatientProfileService profiles)
    : IRequestHandler<GetPatientEncountersByNumberQuery, IReadOnlyList<PatientEncounterSummaryDto>>
{
    public Task<IReadOnlyList<PatientEncounterSummaryDto>> Handle(GetPatientEncountersByNumberQuery request, CancellationToken cancellationToken) =>
        profiles.GetEncountersByNumberAsync(request.PatientNumber, cancellationToken);
}

public sealed class GetPatientEncountersByNumberQueryValidator : AbstractValidator<GetPatientEncountersByNumberQuery>
{
    public GetPatientEncountersByNumberQueryValidator() => RuleFor(x => x.PatientNumber).NotEmpty().MaximumLength(50);
}
