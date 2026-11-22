using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using Hms.Domain.Enums;
using MediatR;

namespace Hms.Application.Features.Billing;

/// <summary>Records a clinic purchase as a debit in the same ledger used by payments.</summary>
public sealed record RecordPurchaseCommand(AccountBookEntryRequest Request) : IRequest<AccountBookEntryDto>;

public sealed class RecordPurchaseCommandHandler(IBillingService billing) : IRequestHandler<RecordPurchaseCommand, AccountBookEntryDto>
{
    public Task<AccountBookEntryDto> Handle(RecordPurchaseCommand request, CancellationToken cancellationToken) =>
        billing.AddExpenseAsync(request.Request with { EntryType = AccountEntryType.Expense }, cancellationToken);
}

public sealed class RecordPurchaseCommandValidator : AbstractValidator<RecordPurchaseCommand>
{
    public RecordPurchaseCommandValidator()
    {
        RuleFor(x => x.Request.Category).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.Amount).GreaterThan(0);
        RuleFor(x => x.Request.Description).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Request.Reference).MaximumLength(200);
    }
}
