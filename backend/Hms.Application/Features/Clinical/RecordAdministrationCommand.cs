using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Clinical;

public sealed record RecordAdministrationCommand(RecordAdministrationRequest Request) : IRequest<Guid>;

public sealed class RecordAdministrationCommandHandler(IClinicalService clinical)
    : IRequestHandler<RecordAdministrationCommand, Guid>
{
    public Task<Guid> Handle(RecordAdministrationCommand request, CancellationToken cancellationToken) =>
        clinical.RecordAdministrationAsync(request.Request, cancellationToken);
}

public sealed class RecordAdministrationCommandValidator : AbstractValidator<RecordAdministrationCommand>
{
    public RecordAdministrationCommandValidator()
    {
        RuleFor(x => x.Request.DispenseItemId).NotEmpty();
        RuleFor(x => x.Request.DoseGiven).GreaterThan(0);
        RuleFor(x => x.Request.Route).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.BatchNumber).MaximumLength(100);
        RuleFor(x => x.Request.Notes).MaximumLength(2000);
    }
}
