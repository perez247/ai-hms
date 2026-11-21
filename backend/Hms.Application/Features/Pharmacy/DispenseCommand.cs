using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Pharmacy;

/// <summary>Issues stock; outpatient bills are committed to Finance, inpatient stock waits for Nurse administration.</summary>
public sealed record DispenseCommand(DispenseRequest Request) : IRequest<DispenseResultDto>;

public sealed class DispenseCommandHandler(IPharmacyService pharmacy) : IRequestHandler<DispenseCommand, DispenseResultDto>
{
    public Task<DispenseResultDto> Handle(DispenseCommand request, CancellationToken cancellationToken) =>
        pharmacy.DispenseAsync(request.Request, cancellationToken);
}

public sealed class DispenseCommandValidator : AbstractValidator<DispenseCommand>
{
    public DispenseCommandValidator()
    {
        RuleFor(x => x.Request.PrescriptionId).NotEmpty();
        RuleFor(x => x.Request.Items).NotEmpty();
        RuleForEach(x => x.Request.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.PrescriptionItemId).NotEmpty();
            item.RuleFor(x => x.Quantity).GreaterThan(0);
        });
    }
}
