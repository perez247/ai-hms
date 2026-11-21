using FluentValidation;
using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Application.Services;
using Hms.Domain.Enums;
using MediatR;

namespace Hms.Application.Features.Billing;

public sealed record RouteBillToHmoCommand(Guid BillId) : IRequest<BillDto>;

public sealed class RouteBillToHmoCommandHandler(IBillingService billing) : IRequestHandler<RouteBillToHmoCommand, BillDto>
{
    public async Task<BillDto> Handle(RouteBillToHmoCommand request, CancellationToken cancellationToken)
    {
        await billing.RouteToHmoAsync(request.BillId, cancellationToken);
        return await billing.GetAsync(request.BillId, cancellationToken);
    }
}

public sealed class RouteBillToHmoCommandValidator : AbstractValidator<RouteBillToHmoCommand>
{
    public RouteBillToHmoCommandValidator() => RuleFor(x => x.BillId).NotEmpty();
}
