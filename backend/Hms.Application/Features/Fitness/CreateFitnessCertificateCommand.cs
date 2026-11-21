using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Fitness;

/// <summary>Nurse creates the draft clearance record and any number of free-form investigation findings.</summary>
public sealed record CreateFitnessCertificateCommand(CreateFitnessCertificateRequest Request) : IRequest<FitnessCertificateDto>;

public sealed class CreateFitnessCertificateCommandHandler(IFitnessService fitness)
    : IRequestHandler<CreateFitnessCertificateCommand, FitnessCertificateDto>
{
    public Task<FitnessCertificateDto> Handle(CreateFitnessCertificateCommand request, CancellationToken cancellationToken) =>
        fitness.CreateAsync(request.Request, cancellationToken);
}

public sealed class CreateFitnessCertificateCommandValidator : AbstractValidator<CreateFitnessCertificateCommand>
{
    public CreateFitnessCertificateCommandValidator()
    {
        RuleFor(x => x.Request.PatientFullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.PatientNumber).MaximumLength(50);
        RuleFor(x => x.Request.PatientAge).InclusiveBetween(0, 130);
        RuleFor(x => x.Request.PersonSex).IsInEnum();
        RuleFor(x => x.Request.Phone).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Request.PaymentType).IsInEnum();
        RuleFor(x => x.Request.HmoCompanyId).NotEmpty().When(x => x.Request.PaymentType == Hms.Domain.Enums.FitnessPaymentType.Hmo);
        RuleFor(x => x.Request.ExpiresAt).GreaterThanOrEqualTo(x => x.Request.FitnessDate).When(x => x.Request.ExpiresAt is not null);
    }
}
