using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Fitness;

/// <summary>Nurse commits the completed certificate for Doctor review.</summary>
public sealed record SubmitFitnessCertificateCommand(Guid CertificateId) : IRequest<FitnessCertificateDto>;

public sealed class SubmitFitnessCertificateCommandHandler(IFitnessService fitness)
    : IRequestHandler<SubmitFitnessCertificateCommand, FitnessCertificateDto>
{
    public Task<FitnessCertificateDto> Handle(SubmitFitnessCertificateCommand request, CancellationToken cancellationToken) =>
        fitness.SubmitAsync(request.CertificateId, cancellationToken);
}

public sealed class SubmitFitnessCertificateCommandValidator : AbstractValidator<SubmitFitnessCertificateCommand>
{
    public SubmitFitnessCertificateCommandValidator() => RuleFor(x => x.CertificateId).NotEmpty();
}
