using FluentValidation;
using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Patients;

/// <summary>Registers a patient, creates the registration period and commits the registration fee bill.</summary>
public sealed record CreatePatientCommand(CreatePatientRequest Request) : IRequest<PatientDto>;

public sealed class CreatePatientCommandHandler(IPatientService patients) : IRequestHandler<CreatePatientCommand, PatientDto>
{
    public Task<PatientDto> Handle(CreatePatientCommand request, CancellationToken cancellationToken) =>
        patients.CreateAsync(request.Request, cancellationToken);
}

public sealed class CreatePatientCommandValidator : AbstractValidator<CreatePatientCommand>
{
    public CreatePatientCommandValidator()
    {
        RuleFor(x => x.Request.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.MiddleName).MaximumLength(100);
        RuleFor(x => x.Request.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.Sex).IsInEnum();
        RuleFor(x => x.Request.DateOfBirth).NotEmpty().LessThan(DateOnly.FromDateTime(DateTime.UtcNow));
        RuleFor(x => x.Request.Phone).MaximumLength(40);
        RuleFor(x => x.Request.Email).EmailAddress().MaximumLength(320).When(x => !string.IsNullOrWhiteSpace(x.Request.Email));
        RuleFor(x => x.Request.HmoEnrollments)
            .Must(enrollments => enrollments is null || enrollments.Count(x => x.IsPrimary) <= 1)
            .WithMessage("Only one HMO enrollment may be primary.");
        When(x => x.Request.HmoEnrollments is not null, () =>
        {
            RuleForEach(x => x.Request.HmoEnrollments!).ChildRules(enrollment =>
            {
                enrollment.RuleFor(x => x.HmoCompanyId).NotEmpty();
                enrollment.RuleFor(x => x.HmoMemberId).NotEmpty().MaximumLength(100);
                enrollment.RuleFor(x => x.PlanName).MaximumLength(100);
                enrollment.RuleFor(x => x.ValidTo)
                    .GreaterThanOrEqualTo(x => x.ValidFrom)
                    .When(x => x.ValidTo is not null);
            });
        });
    }
}
