using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Clinical;

public sealed record CompleteSurgeryCommand(Guid SurgeryId, CompleteSurgeryRequest Request) : IRequest<SurgerySummaryDto>;

public sealed class CompleteSurgeryCommandHandler(IClinicalService clinical)
    : IRequestHandler<CompleteSurgeryCommand, SurgerySummaryDto>
{
    public Task<SurgerySummaryDto> Handle(CompleteSurgeryCommand request, CancellationToken cancellationToken) =>
        clinical.CompleteSurgeryAsync(request.SurgeryId, request.Request, cancellationToken);
}

public sealed class CompleteSurgeryCommandValidator : AbstractValidator<CompleteSurgeryCommand>
{
    public CompleteSurgeryCommandValidator()
    {
        RuleFor(x => x.SurgeryId).NotEmpty();
        RuleFor(x => x.Request.ReportText).NotEmpty().MaximumLength(20000);
        RuleForEach(x => x.Request.ParticipantUserIds).NotEmpty();
    }
}
