using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Nursing;

public sealed record GetOpenNurseEncountersQuery(string PatientNumber) : IRequest<IReadOnlyList<NurseEncounterDto>>;

public sealed class GetOpenNurseEncountersQueryHandler(INurseService nurse)
    : IRequestHandler<GetOpenNurseEncountersQuery, IReadOnlyList<NurseEncounterDto>>
{
    public Task<IReadOnlyList<NurseEncounterDto>> Handle(GetOpenNurseEncountersQuery request, CancellationToken cancellationToken) =>
        nurse.GetOpenEncountersAsync(request.PatientNumber, cancellationToken);
}

public sealed class GetOpenNurseEncountersQueryValidator : AbstractValidator<GetOpenNurseEncountersQuery>
{
    public GetOpenNurseEncountersQueryValidator() => RuleFor(x => x.PatientNumber).NotEmpty().MaximumLength(50);
}
