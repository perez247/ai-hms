using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Billing;

public sealed record CreateManualBillCommand(CreateManualBillRequest Request) : IRequest<ManualBillResultDto>;

public sealed class CreateManualBillCommandHandler(IBillingService billing) : IRequestHandler<CreateManualBillCommand, ManualBillResultDto>
{
    public Task<ManualBillResultDto> Handle(CreateManualBillCommand request, CancellationToken cancellationToken) =>
        billing.CreateManualBillAsync(request.Request, cancellationToken);
}

public sealed class CreateManualBillCommandValidator : AbstractValidator<CreateManualBillCommand>
{
    public CreateManualBillCommandValidator()
    {
        RuleFor(x => x.Request.PatientId).NotEmpty();
        RuleFor(x => x.Request.Lines).NotEmpty();
        RuleForEach(x => x.Request.Lines).ChildRules(line =>
        {
            line.RuleFor(x => x.Description).NotEmpty().MaximumLength(200);
            line.RuleFor(x => x.Quantity).GreaterThan(0);
            line.RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        });
    }
}
