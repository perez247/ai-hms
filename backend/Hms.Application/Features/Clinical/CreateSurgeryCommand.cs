using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Clinical;

public sealed record CreateSurgeryCommand(CreateSurgeryRequest Request) : IRequest<SurgerySummaryDto>;

public sealed class CreateSurgeryCommandHandler(IClinicalService clinical) : IRequestHandler<CreateSurgeryCommand, SurgerySummaryDto>
{
    public Task<SurgerySummaryDto> Handle(CreateSurgeryCommand request, CancellationToken cancellationToken) =>
        clinical.CreateSurgeryAsync(request.Request, cancellationToken);
}

public sealed class CreateSurgeryCommandValidator : AbstractValidator<CreateSurgeryCommand>
{
    public CreateSurgeryCommandValidator()
    {
        RuleFor(x => x.Request.EncounterId).NotEmpty();
        RuleFor(x => x.Request.CatalogItemId).NotEmpty();
        RuleFor(x => x.Request.Theater).MaximumLength(100);
    }
}
