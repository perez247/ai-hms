using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Clinical;

public sealed record CompleteInvestigationCommand(Guid InvestigationId, CompleteInvestigationRequest Request) : IRequest<InvestigationSummaryDto>;

public sealed class CompleteInvestigationCommandHandler(IClinicalService clinical)
    : IRequestHandler<CompleteInvestigationCommand, InvestigationSummaryDto>
{
    public Task<InvestigationSummaryDto> Handle(CompleteInvestigationCommand request, CancellationToken cancellationToken) =>
        clinical.CompleteInvestigationAsync(request.InvestigationId, request.Request, cancellationToken);
}

public sealed class CompleteInvestigationCommandValidator : AbstractValidator<CompleteInvestigationCommand>
{
    public CompleteInvestigationCommandValidator()
    {
        RuleFor(x => x.InvestigationId).NotEmpty();
        RuleFor(x => x.Request.ReportText).NotEmpty().MaximumLength(20000);
        RuleForEach(x => x.Request.ParticipantUserIds).NotEmpty();
    }
}
