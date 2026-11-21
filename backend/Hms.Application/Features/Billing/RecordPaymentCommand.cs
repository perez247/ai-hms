using FluentValidation;
using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Application.Services;
using Hms.Domain.Enums;
using MediatR;

namespace Hms.Application.Features.Billing;

/// <summary>Records patient/HMO money, posts the account-book entry and settles the bill when fully paid.</summary>
public sealed record RecordPaymentCommand(Guid BillId, PaymentRequest Request) : IRequest<BillDto>;

public sealed class RecordPaymentCommandHandler(IBillingService billing) : IRequestHandler<RecordPaymentCommand, BillDto>
{
    public async Task<BillDto> Handle(RecordPaymentCommand request, CancellationToken cancellationToken)
    {
        await billing.RecordPaymentAsync(request.BillId, request.Request, cancellationToken);
        return await billing.GetAsync(request.BillId, cancellationToken);
    }
}

public sealed class RecordPaymentCommandValidator : AbstractValidator<RecordPaymentCommand>
{
    public RecordPaymentCommandValidator()
    {
        RuleFor(x => x.BillId).NotEmpty();
        RuleFor(x => x.Request.Amount).GreaterThan(0);
        RuleFor(x => x.Request.Method).IsInEnum();
        RuleFor(x => x.Request.Reference).MaximumLength(200);
        RuleFor(x => x.Request.IdempotencyKey).MaximumLength(200);
    }
}
