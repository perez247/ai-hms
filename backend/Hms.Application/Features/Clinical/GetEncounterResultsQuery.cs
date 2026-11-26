using FluentValidation;
using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Clinical;

public sealed record GetEncounterResultsQuery(Guid EncounterId, int Page, int PageSize)
    : IRequest<PagedResult<ServiceRequestSummaryDto>>;

public sealed class GetEncounterResultsQueryHandler(IClinicalService clinical)
    : IRequestHandler<GetEncounterResultsQuery, PagedResult<ServiceRequestSummaryDto>>
{
    public Task<PagedResult<ServiceRequestSummaryDto>> Handle(GetEncounterResultsQuery request, CancellationToken cancellationToken) =>
        clinical.GetEncounterResultsAsync(request.EncounterId, request.Page, request.PageSize, cancellationToken);
}

public sealed class GetEncounterResultsQueryValidator : AbstractValidator<GetEncounterResultsQuery>
{
    public GetEncounterResultsQueryValidator()
    {
        RuleFor(x => x.EncounterId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PageSize).GreaterThanOrEqualTo(0);
    }
}
