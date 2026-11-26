using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Clinical;

public sealed record AddInpatientEncounterCommand(CreateInpatientEncounterRequest Request) : IRequest<EncounterDto>;

public sealed class AddInpatientEncounterCommandHandler(IClinicalService clinical) : IRequestHandler<AddInpatientEncounterCommand, EncounterDto>
{
    public Task<EncounterDto> Handle(AddInpatientEncounterCommand request, CancellationToken cancellationToken) =>
        clinical.AddInpatientEncounterAsync(request.Request, cancellationToken);
}

public sealed class AddInpatientEncounterCommandValidator : AbstractValidator<AddInpatientEncounterCommand>
{
    public AddInpatientEncounterCommandValidator()
    {
        RuleFor(x => x.Request.AdmissionId).NotEmpty();
        RuleFor(x => x.Request.DoctorId).NotEmpty();
        RuleFor(x => x.Request.PresentingComplaint).MaximumLength(1000);
    }
}
