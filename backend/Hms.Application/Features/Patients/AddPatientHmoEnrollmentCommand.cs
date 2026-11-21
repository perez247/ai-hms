using FluentValidation;
using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Patients;

public sealed record AddPatientHmoEnrollmentCommand(Guid PatientId, AddHmoEnrollmentRequest Request) : IRequest<PatientHmoEnrollmentDto>;

public sealed class AddPatientHmoEnrollmentCommandHandler(IPatientService patients)
    : IRequestHandler<AddPatientHmoEnrollmentCommand, PatientHmoEnrollmentDto>
{
    public Task<PatientHmoEnrollmentDto> Handle(AddPatientHmoEnrollmentCommand request, CancellationToken cancellationToken) =>
        patients.AddHmoEnrollmentAsync(request.PatientId, request.Request, cancellationToken);
}

public sealed class AddPatientHmoEnrollmentCommandValidator : AbstractValidator<AddPatientHmoEnrollmentCommand>
{
    public AddPatientHmoEnrollmentCommandValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.Request.HmoCompanyId).NotEmpty();
        RuleFor(x => x.Request.HmoMemberId).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.PlanName).MaximumLength(100);
        RuleFor(x => x.Request.ValidTo).GreaterThanOrEqualTo(x => x.Request.ValidFrom).When(x => x.Request.ValidTo is not null);
    }
}
