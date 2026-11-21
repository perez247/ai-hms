using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Clinical;

public sealed record GetEncounterQuery(Guid EncounterId) : IRequest<EncounterDto>;

public sealed class GetEncounterQueryHandler(IClinicalService clinical) : IRequestHandler<GetEncounterQuery, EncounterDto>
{
    public Task<EncounterDto> Handle(GetEncounterQuery request, CancellationToken cancellationToken) =>
        clinical.GetEncounterAsync(request.EncounterId, cancellationToken);
}

public sealed class GetEncounterQueryValidator : AbstractValidator<GetEncounterQuery>
{
    public GetEncounterQueryValidator() => RuleFor(x => x.EncounterId).NotEmpty();
}
