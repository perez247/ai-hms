using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Clinical;

public sealed record CreateOutpatientEncounterCommand(CreateOutpatientEncounterRequest Request) : IRequest<EncounterDto>;

public sealed class CreateOutpatientEncounterCommandHandler(IClinicalService clinical)
    : IRequestHandler<CreateOutpatientEncounterCommand, EncounterDto>
{
    public Task<EncounterDto> Handle(CreateOutpatientEncounterCommand request, CancellationToken cancellationToken) =>
        clinical.CreateOutpatientEncounterAsync(request.Request, cancellationToken);
}

public sealed class CreateOutpatientEncounterCommandValidator : AbstractValidator<CreateOutpatientEncounterCommand>
{
    public CreateOutpatientEncounterCommandValidator()
    {
        RuleFor(x => x.Request.PatientId).NotEmpty();
        RuleFor(x => x.Request.DoctorId).NotEmpty();
        RuleFor(x => x.Request.PresentingComplaint).MaximumLength(1000);
    }
}
