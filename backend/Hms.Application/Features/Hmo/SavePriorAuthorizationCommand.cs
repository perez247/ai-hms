using FluentValidation;
using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Hmo;

/// <summary>Captures the payer PA code and links it to the matching encounter/admission bill.</summary>
public sealed record SavePriorAuthorizationCommand(SavePriorAuthorizationRequest Request) : IRequest<PriorAuthorizationDto>;

public sealed class SavePriorAuthorizationCommandHandler(IHmoService hmo)
    : IRequestHandler<SavePriorAuthorizationCommand, PriorAuthorizationDto>
{
    public Task<PriorAuthorizationDto> Handle(SavePriorAuthorizationCommand request, CancellationToken cancellationToken) =>
        hmo.SavePriorAuthorizationAsync(request.Request, cancellationToken);
}

public sealed class SavePriorAuthorizationCommandValidator : AbstractValidator<SavePriorAuthorizationCommand>
{
    public SavePriorAuthorizationCommandValidator()
    {
        RuleFor(x => x.Request.HmoCompanyId).NotEmpty();
        RuleFor(x => x.Request.HmoEnrollmentId).NotEmpty();
        RuleFor(x => x.Request.PaCode).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request)
            .Must(x => (x.EncounterId is not null) ^ (x.AdmissionId is not null))
            .WithMessage("A PA must reference exactly one encounter or admission.");
        RuleFor(x => x.Request.Notes).MaximumLength(2000);
    }
}
