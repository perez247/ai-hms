using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Billing;

/// <summary>Records external funds as a credit in the shared account-book ledger.</summary>
public sealed record RecordCreditCommand(RecordCreditRequest Request) : IRequest<AccountBookEntryDto>;

public sealed class RecordCreditCommandHandler(IBillingService billing)
    : IRequestHandler<RecordCreditCommand, AccountBookEntryDto>
{
    public Task<AccountBookEntryDto> Handle(RecordCreditCommand request, CancellationToken cancellationToken) =>
        billing.AddCreditAsync(request.Request, cancellationToken);
}

public sealed class RecordCreditCommandValidator : AbstractValidator<RecordCreditCommand>
{
    public RecordCreditCommandValidator()
    {
        RuleFor(x => x.Request.Amount).GreaterThan(0);
        RuleFor(x => x.Request.Source).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.Description).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Request.Reference).MaximumLength(200);
    }
}
