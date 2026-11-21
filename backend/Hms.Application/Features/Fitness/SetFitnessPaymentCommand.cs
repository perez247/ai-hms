using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Fitness;

/// <summary>Finance marks an approved clearance paid or unpaid.</summary>
public sealed record SetFitnessPaymentCommand(Guid CertificateId, FitnessPaymentRequest Request) : IRequest<FitnessCertificateDto>;

public sealed class SetFitnessPaymentCommandHandler(IFitnessService fitness)
    : IRequestHandler<SetFitnessPaymentCommand, FitnessCertificateDto>
{
    public Task<FitnessCertificateDto> Handle(SetFitnessPaymentCommand request, CancellationToken cancellationToken) =>
        fitness.SetPaymentAsync(request.CertificateId, request.Request, cancellationToken);
}

public sealed class SetFitnessPaymentCommandValidator : AbstractValidator<SetFitnessPaymentCommand>
{
    public SetFitnessPaymentCommandValidator() => RuleFor(x => x.CertificateId).NotEmpty();
}
