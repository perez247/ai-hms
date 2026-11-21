using FluentValidation;
using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Application.Services;
using Hms.Domain.Enums;
using MediatR;

namespace Hms.Application.Features.Billing;

public sealed record GetAccountBookQuery(DateOnly? From, DateOnly? To, AccountEntryType? Type, int Page, int PageSize)
    : IRequest<PagedResult<AccountBookEntryDto>>;

public sealed class GetAccountBookQueryHandler(IBillingService billing)
    : IRequestHandler<GetAccountBookQuery, PagedResult<AccountBookEntryDto>>
{
    public Task<PagedResult<AccountBookEntryDto>> Handle(GetAccountBookQuery request, CancellationToken cancellationToken) =>
        billing.GetAccountBookAsync(request.From, request.To, request.Type, request.Page, request.PageSize, cancellationToken);
}

public sealed class GetAccountBookQueryValidator : AbstractValidator<GetAccountBookQuery>
{
    public GetAccountBookQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PageSize).GreaterThanOrEqualTo(0);
        RuleFor(x => x).Must(x => x.From is null || x.To is null || x.From <= x.To)
            .WithMessage("'From' must be on or before 'To'.");
    }
}
