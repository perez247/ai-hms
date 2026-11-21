using FluentValidation;
using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Application.Services;
using Hms.Domain.Enums;
using MediatR;

namespace Hms.Application.Features.Billing;

public sealed record SearchBillsQuery(BillStatus? Status, Guid? PatientId, int Page, int PageSize) : IRequest<PagedResult<BillDto>>;

public sealed class SearchBillsQueryHandler(IBillingService billing) : IRequestHandler<SearchBillsQuery, PagedResult<BillDto>>
{
    public Task<PagedResult<BillDto>> Handle(SearchBillsQuery request, CancellationToken cancellationToken) =>
        billing.SearchAsync(request.Status, request.PatientId, request.Page, request.PageSize, cancellationToken);
}

public sealed class SearchBillsQueryValidator : AbstractValidator<SearchBillsQuery>
{
    public SearchBillsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PageSize).GreaterThanOrEqualTo(0);
    }
}
