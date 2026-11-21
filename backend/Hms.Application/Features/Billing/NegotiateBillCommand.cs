using FluentValidation;
using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Application.Services;
using Hms.Domain.Enums;
using MediatR;

namespace Hms.Application.Features.Billing;

public sealed record NegotiateBillCommand(Guid BillId, NegotiateBillRequest Request) : IRequest<BillDto>;

public sealed class NegotiateBillCommandHandler(IBillingService billing) : IRequestHandler<NegotiateBillCommand, BillDto>
{
    public async Task<BillDto> Handle(NegotiateBillCommand request, CancellationToken cancellationToken)
    {
        await billing.NegotiateAsync(request.BillId, request.Request, cancellationToken);
        return await billing.GetAsync(request.BillId, cancellationToken);
    }
}

public sealed class NegotiateBillCommandValidator : AbstractValidator<NegotiateBillCommand>
{
    public NegotiateBillCommandValidator()
    {
        RuleFor(x => x.BillId).NotEmpty();
        RuleFor(x => x.Request.DiscountAmount).GreaterThan(0);
        RuleFor(x => x.Request.Reason).NotEmpty().MaximumLength(1000);
    }
}
