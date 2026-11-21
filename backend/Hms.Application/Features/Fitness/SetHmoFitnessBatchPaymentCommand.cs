using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Fitness;

/// <summary>Finance bulk-settles approved HMO fitness records from the HMO profile.</summary>
public sealed record SetHmoFitnessBatchPaymentCommand(FitnessBatchPaymentRequest Request) : IRequest<FitnessCertificateDto[]>;

public sealed class SetHmoFitnessBatchPaymentCommandHandler(IFitnessService fitness)
    : IRequestHandler<SetHmoFitnessBatchPaymentCommand, FitnessCertificateDto[]>
{
    public Task<FitnessCertificateDto[]> Handle(SetHmoFitnessBatchPaymentCommand request, CancellationToken cancellationToken) =>
        fitness.SetHmoBatchPaymentAsync(request.Request, cancellationToken);
}

public sealed class SetHmoFitnessBatchPaymentCommandValidator : AbstractValidator<SetHmoFitnessBatchPaymentCommand>
{
    public SetHmoFitnessBatchPaymentCommandValidator()
    {
        RuleFor(x => x.Request.HmoCompanyId).NotEmpty();
        RuleFor(x => x.Request.CertificateIds).NotEmpty();
        RuleForEach(x => x.Request.CertificateIds).NotEmpty();
    }
}
