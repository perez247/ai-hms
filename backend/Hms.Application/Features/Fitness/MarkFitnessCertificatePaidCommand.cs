using FluentValidation;
using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Application.Services;
using Hms.Domain.Enums;
using MediatR;

namespace Hms.Application.Features.Fitness;

/// <summary>Legacy paid shortcut; delegates to the approved-only payment transition.</summary>
public sealed record MarkFitnessCertificatePaidCommand(Guid CertificateId) : IRequest<FitnessCertificateDto>;

public sealed class MarkFitnessCertificatePaidCommandHandler(IFitnessService fitness)
    : IRequestHandler<MarkFitnessCertificatePaidCommand, FitnessCertificateDto>
{
    public Task<FitnessCertificateDto> Handle(MarkFitnessCertificatePaidCommand request, CancellationToken cancellationToken) =>
        fitness.MarkPaidAsync(request.CertificateId, cancellationToken);
}

public sealed class MarkFitnessCertificatePaidCommandValidator : AbstractValidator<MarkFitnessCertificatePaidCommand>
{
    public MarkFitnessCertificatePaidCommandValidator() => RuleFor(x => x.CertificateId).NotEmpty();
}
