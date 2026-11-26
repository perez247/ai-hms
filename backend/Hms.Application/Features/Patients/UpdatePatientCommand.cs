using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Patients;

public sealed record UpdatePatientCommand(Guid Id, UpdatePatientRequest Request) : IRequest<PatientDto>;

public sealed class UpdatePatientCommandHandler(IPatientService patients) : IRequestHandler<UpdatePatientCommand, PatientDto>
{
    public Task<PatientDto> Handle(UpdatePatientCommand request, CancellationToken cancellationToken) =>
        patients.UpdateAsync(request.Id, request.Request, cancellationToken);
}

public sealed class UpdatePatientCommandValidator : AbstractValidator<UpdatePatientCommand>
{
    public UpdatePatientCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Request.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.MiddleName).MaximumLength(100);
        RuleFor(x => x.Request.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.Sex).IsInEnum();
        RuleFor(x => x.Request.DateOfBirth).NotEmpty().LessThan(DateOnly.FromDateTime(DateTime.UtcNow));
        RuleFor(x => x.Request.Phone).MaximumLength(40);
        RuleFor(x => x.Request.Email).EmailAddress().MaximumLength(320).When(x => !string.IsNullOrWhiteSpace(x.Request.Email));
        RuleFor(x => x.Request.StateOfOrigin).MaximumLength(100);
        RuleFor(x => x.Request.HomeTown).MaximumLength(100);
        RuleFor(x => x.Request.Allergy).MaximumLength(200);
        RuleFor(x => x.Request.Occupation).MaximumLength(100);
        RuleFor(x => x.Request.NextOfKinName).MaximumLength(100);
        RuleFor(x => x.Request.NextOfKinPhone).MaximumLength(40);
    }
}
