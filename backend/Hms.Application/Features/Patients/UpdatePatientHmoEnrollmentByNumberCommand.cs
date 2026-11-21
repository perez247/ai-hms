using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Patients;

public sealed record UpdatePatientHmoEnrollmentByNumberCommand(string PatientNumber, Guid EnrollmentId, AddHmoEnrollmentRequest Request)
    : IRequest<PatientHmoEnrollmentDto>;

public sealed class UpdatePatientHmoEnrollmentByNumberCommandHandler(IPatientService patients)
    : IRequestHandler<UpdatePatientHmoEnrollmentByNumberCommand, PatientHmoEnrollmentDto>
{
    public Task<PatientHmoEnrollmentDto> Handle(UpdatePatientHmoEnrollmentByNumberCommand request, CancellationToken cancellationToken) =>
        patients.UpdateHmoEnrollmentByNumberAsync(request.PatientNumber, request.EnrollmentId, request.Request, cancellationToken);
}

public sealed class UpdatePatientHmoEnrollmentByNumberCommandValidator : AbstractValidator<UpdatePatientHmoEnrollmentByNumberCommand>
{
    public UpdatePatientHmoEnrollmentByNumberCommandValidator()
    {
        RuleFor(x => x.PatientNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.EnrollmentId).NotEmpty();
        RuleFor(x => x.Request.HmoCompanyId).NotEmpty();
        RuleFor(x => x.Request.HmoMemberId).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.ValidTo).GreaterThanOrEqualTo(x => x.Request.ValidFrom).When(x => x.Request.ValidTo is not null);
    }
}
