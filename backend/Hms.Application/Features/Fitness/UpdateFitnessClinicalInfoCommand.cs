using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Fitness;

/// <summary>Nurse enters/replaces the free-form investigation findings for an awaiting record.</summary>
public sealed record UpdateFitnessClinicalInfoCommand(Guid CertificateId, UpdateFitnessClinicalInfoRequest Request)
    : IRequest<FitnessCertificateDto>;

public sealed class UpdateFitnessClinicalInfoCommandHandler(IFitnessService fitness)
    : IRequestHandler<UpdateFitnessClinicalInfoCommand, FitnessCertificateDto>
{
    public Task<FitnessCertificateDto> Handle(UpdateFitnessClinicalInfoCommand request, CancellationToken cancellationToken) =>
        fitness.UpdateClinicalInfoAsync(request.CertificateId, request.Request, cancellationToken);
}

public sealed class UpdateFitnessClinicalInfoCommandValidator : AbstractValidator<UpdateFitnessClinicalInfoCommand>
{
    public UpdateFitnessClinicalInfoCommandValidator()
    {
        RuleFor(x => x.CertificateId).NotEmpty();
        RuleForEach(x => x.Request.InvestigationResults).ChildRules(result =>
        {
            result.RuleFor(x => x.Key).NotEmpty().MaximumLength(200);
            result.RuleFor(x => x.Value).NotEmpty().MaximumLength(4000);
        });
    }
}
