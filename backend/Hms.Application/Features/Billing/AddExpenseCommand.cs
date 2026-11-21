using FluentValidation;
using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Application.Services;
using Hms.Domain.Enums;
using MediatR;

namespace Hms.Application.Features.Billing;

public sealed record AddExpenseCommand(AccountBookEntryRequest Request) : IRequest<AccountBookEntryDto>;

public sealed class AddExpenseCommandHandler(IBillingService billing) : IRequestHandler<AddExpenseCommand, AccountBookEntryDto>
{
    public Task<AccountBookEntryDto> Handle(AddExpenseCommand request, CancellationToken cancellationToken) =>
        billing.AddExpenseAsync(request.Request, cancellationToken);
}

public sealed class AddExpenseCommandValidator : AbstractValidator<AddExpenseCommand>
{
    public AddExpenseCommandValidator()
    {
        RuleFor(x => x.Request.EntryType).Equal(AccountEntryType.Expense);
        RuleFor(x => x.Request.Category).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.Amount).GreaterThan(0);
        RuleFor(x => x.Request.Description).NotEmpty().MaximumLength(1000);
    }
}
