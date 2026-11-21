using FluentValidation;
using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Patients;

public sealed record RenewPatientRegistrationCommand(Guid PatientId) : IRequest<RegistrationRenewalResult>;

public sealed class RenewPatientRegistrationCommandHandler(IPatientService patients)
    : IRequestHandler<RenewPatientRegistrationCommand, RegistrationRenewalResult>
{
    public Task<RegistrationRenewalResult> Handle(RenewPatientRegistrationCommand request, CancellationToken cancellationToken) =>
        patients.RenewRegistrationAsync(request.PatientId, cancellationToken);
}

public sealed class RenewPatientRegistrationCommandValidator : AbstractValidator<RenewPatientRegistrationCommand>
{
    public RenewPatientRegistrationCommandValidator() => RuleFor(x => x.PatientId).NotEmpty();
}
