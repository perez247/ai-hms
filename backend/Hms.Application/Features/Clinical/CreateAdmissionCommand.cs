using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Clinical;

public sealed record CreateAdmissionCommand(CreateAdmissionRequest Request) : IRequest<AdmissionDto>;

public sealed class CreateAdmissionCommandHandler(IClinicalService clinical) : IRequestHandler<CreateAdmissionCommand, AdmissionDto>
{
    public Task<AdmissionDto> Handle(CreateAdmissionCommand request, CancellationToken cancellationToken) =>
        clinical.CreateAdmissionAsync(request.Request, cancellationToken);
}

public sealed class CreateAdmissionCommandValidator : AbstractValidator<CreateAdmissionCommand>
{
    public CreateAdmissionCommandValidator()
    {
        RuleFor(x => x.Request.PatientId).NotEmpty();
        RuleFor(x => x.Request.DoctorId).NotEmpty();
        RuleFor(x => x.Request.WardType).IsInEnum();
        RuleFor(x => x.Request.BedNumber).MaximumLength(40);
        RuleFor(x => x.Request.ChiefComplaint).MaximumLength(2000);
    }
}
