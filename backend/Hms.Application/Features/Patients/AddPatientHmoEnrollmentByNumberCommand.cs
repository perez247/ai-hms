using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Patients;

public sealed record AddPatientHmoEnrollmentByNumberCommand(string PatientNumber, AddHmoEnrollmentRequest Request)
    : IRequest<PatientHmoEnrollmentDto>;

public sealed class AddPatientHmoEnrollmentByNumberCommandHandler(IPatientService patients)
    : IRequestHandler<AddPatientHmoEnrollmentByNumberCommand, PatientHmoEnrollmentDto>
{
    public Task<PatientHmoEnrollmentDto> Handle(AddPatientHmoEnrollmentByNumberCommand request, CancellationToken cancellationToken) =>
        patients.AddHmoEnrollmentByNumberAsync(request.PatientNumber, request.Request, cancellationToken);
}

public sealed class AddPatientHmoEnrollmentByNumberCommandValidator : AbstractValidator<AddPatientHmoEnrollmentByNumberCommand>
{
    public AddPatientHmoEnrollmentByNumberCommandValidator()
    {
        RuleFor(x => x.PatientNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Request.HmoCompanyId).NotEmpty();
        RuleFor(x => x.Request.HmoMemberId).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.ValidTo).GreaterThanOrEqualTo(x => x.Request.ValidFrom).When(x => x.Request.ValidTo is not null);
    }
}
