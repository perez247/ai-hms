using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Billing;

/// <summary>Moves an unpaid bill to the debt ledger: the patient owes for self-pay, the HMO for HMO bills.</summary>
public sealed record MoveBillToDebtCommand(Guid BillId, MoveBillToDebtRequest Request) : IRequest<BillDto>;

public sealed class MoveBillToDebtCommandHandler(IBillingService billing) : IRequestHandler<MoveBillToDebtCommand, BillDto>
{
    public async Task<BillDto> Handle(MoveBillToDebtCommand request, CancellationToken cancellationToken)
    {
        await billing.MoveToDebtAsync(request.BillId, request.Request.Reason, cancellationToken);
        return await billing.GetAsync(request.BillId, cancellationToken);
    }
}

public sealed class MoveBillToDebtCommandValidator : AbstractValidator<MoveBillToDebtCommand>
{
    public MoveBillToDebtCommandValidator()
    {
        RuleFor(x => x.BillId).NotEmpty();
        RuleFor(x => x.Request.Reason).MaximumLength(1000);
    }
}
