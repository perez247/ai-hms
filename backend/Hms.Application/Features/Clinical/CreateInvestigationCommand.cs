using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Clinical;

public sealed record CreateInvestigationCommand(CreateInvestigationRequest Request) : IRequest<InvestigationSummaryDto>;

public sealed class CreateInvestigationCommandHandler(IClinicalService clinical)
    : IRequestHandler<CreateInvestigationCommand, InvestigationSummaryDto>
{
    public Task<InvestigationSummaryDto> Handle(CreateInvestigationCommand request, CancellationToken cancellationToken) =>
        clinical.CreateInvestigationAsync(request.Request, cancellationToken);
}

public sealed class CreateInvestigationCommandValidator : AbstractValidator<CreateInvestigationCommand>
{
    public CreateInvestigationCommandValidator()
    {
        RuleFor(x => x.Request.EncounterId).NotEmpty();
        RuleFor(x => x.Request.CatalogItemId).NotEmpty();
    }
}
