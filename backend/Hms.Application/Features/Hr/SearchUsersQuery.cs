using FluentValidation;
using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Hr;

public sealed record SearchUsersQuery(bool? Active, int Page, int PageSize) : IRequest<PagedResult<UserDto>>;

public sealed class SearchUsersQueryHandler(IHrService hr) : IRequestHandler<SearchUsersQuery, PagedResult<UserDto>>
{
    public Task<PagedResult<UserDto>> Handle(SearchUsersQuery request, CancellationToken cancellationToken) =>
        hr.SearchAsync(request.Active, request.Page, request.PageSize, cancellationToken);
}

public sealed class SearchUsersQueryValidator : AbstractValidator<SearchUsersQuery>
{
    public SearchUsersQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PageSize).GreaterThanOrEqualTo(0);
    }
}
