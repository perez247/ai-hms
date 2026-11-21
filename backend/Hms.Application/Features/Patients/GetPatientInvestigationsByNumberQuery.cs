using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Patients;

public sealed record GetPatientInvestigationsByNumberQuery(string PatientNumber) : IRequest<IReadOnlyList<PatientInvestigationDto>>;

public sealed class GetPatientInvestigationsByNumberQueryHandler(IPatientProfileService profiles)
    : IRequestHandler<GetPatientInvestigationsByNumberQuery, IReadOnlyList<PatientInvestigationDto>>
{
    public Task<IReadOnlyList<PatientInvestigationDto>> Handle(GetPatientInvestigationsByNumberQuery request, CancellationToken cancellationToken) =>
        profiles.GetInvestigationsByNumberAsync(request.PatientNumber, cancellationToken);
}

public sealed class GetPatientInvestigationsByNumberQueryValidator : AbstractValidator<GetPatientInvestigationsByNumberQuery>
{
    public GetPatientInvestigationsByNumberQueryValidator() => RuleFor(x => x.PatientNumber).NotEmpty().MaximumLength(50);
}
