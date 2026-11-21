using FluentValidation;
using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Application.Services;
using Hms.Domain.Enums;
using MediatR;

namespace Hms.Application.Features.Billing;

public sealed record GetBillQuery(Guid BillId) : IRequest<BillDto>;

public sealed class GetBillQueryHandler(IBillingService billing) : IRequestHandler<GetBillQuery, BillDto>
{
    public Task<BillDto> Handle(GetBillQuery request, CancellationToken cancellationToken) =>
        billing.GetAsync(request.BillId, cancellationToken);
}

public sealed class GetBillQueryValidator : AbstractValidator<GetBillQuery>
{
    public GetBillQueryValidator() => RuleFor(x => x.BillId).NotEmpty();
}
