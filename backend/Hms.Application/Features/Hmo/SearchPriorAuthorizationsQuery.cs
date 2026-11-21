using FluentValidation;
using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Hmo;

public sealed record SearchPriorAuthorizationsQuery(Guid? PatientId, int Page, int PageSize) : IRequest<PagedResult<PriorAuthorizationDto>>;

public sealed class SearchPriorAuthorizationsQueryHandler(IHmoService hmo)
    : IRequestHandler<SearchPriorAuthorizationsQuery, PagedResult<PriorAuthorizationDto>>
{
    public Task<PagedResult<PriorAuthorizationDto>> Handle(SearchPriorAuthorizationsQuery request, CancellationToken cancellationToken) =>
        hmo.SearchAuthorizationsAsync(request.PatientId, request.Page, request.PageSize, cancellationToken);
}

public sealed class SearchPriorAuthorizationsQueryValidator : AbstractValidator<SearchPriorAuthorizationsQuery>
{
    public SearchPriorAuthorizationsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PageSize).GreaterThanOrEqualTo(0);
    }
}
