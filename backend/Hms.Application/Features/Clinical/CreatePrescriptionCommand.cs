using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Clinical;

public sealed record CreatePrescriptionCommand(CreatePrescriptionRequest Request) : IRequest<PrescriptionSummaryDto>;

public sealed class CreatePrescriptionCommandHandler(IClinicalService clinical)
    : IRequestHandler<CreatePrescriptionCommand, PrescriptionSummaryDto>
{
    public Task<PrescriptionSummaryDto> Handle(CreatePrescriptionCommand request, CancellationToken cancellationToken) =>
        clinical.CreatePrescriptionAsync(request.Request, cancellationToken);
}

public sealed class CreatePrescriptionCommandValidator : AbstractValidator<CreatePrescriptionCommand>
{
    public CreatePrescriptionCommandValidator()
    {
        RuleFor(x => x.Request.EncounterId).NotEmpty();
        RuleFor(x => x.Request.Notes).MaximumLength(2000);
        RuleFor(x => x.Request.Items).NotEmpty();
        RuleForEach(x => x.Request.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.MedicationId).NotEmpty();
            item.RuleFor(x => x.Dose).NotEmpty().MaximumLength(100);
            item.RuleFor(x => x.Route).NotEmpty().MaximumLength(100);
            item.RuleFor(x => x.Frequency).NotEmpty().MaximumLength(100);
            item.RuleFor(x => x.DurationDays).GreaterThan(0);
            item.RuleFor(x => x.Quantity).GreaterThan(0);
            item.RuleFor(x => x.Instructions).MaximumLength(1000);
        });
    }
}
