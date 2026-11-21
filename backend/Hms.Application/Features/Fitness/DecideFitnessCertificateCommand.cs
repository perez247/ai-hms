using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Fitness;

/// <summary>Doctor decision on a submitted pre-procedure clearance.</summary>
public sealed record DecideFitnessCertificateCommand(Guid CertificateId, FitnessDecisionRequest Request) : IRequest<FitnessCertificateDto>;

public sealed class DecideFitnessCertificateCommandHandler(IFitnessService fitness)
    : IRequestHandler<DecideFitnessCertificateCommand, FitnessCertificateDto>
{
    public Task<FitnessCertificateDto> Handle(DecideFitnessCertificateCommand request, CancellationToken cancellationToken) =>
        fitness.DecideAsync(request.CertificateId, request.Request, cancellationToken);
}

public sealed class DecideFitnessCertificateCommandValidator : AbstractValidator<DecideFitnessCertificateCommand>
{
    public DecideFitnessCertificateCommandValidator()
    {
        RuleFor(x => x.CertificateId).NotEmpty();
        RuleFor(x => x.Request.RejectionReason).NotEmpty().MaximumLength(2000).When(x => !x.Request.Approve);
    }
}
