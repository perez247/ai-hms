using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Hmo;

public sealed record SaveHmoTariffCommand(Guid HmoCompanyId, CreateHmoTariffRequest Request) : IRequest<HmoTariffDto>;

public sealed class SaveHmoTariffCommandHandler(IHmoService hmo) : IRequestHandler<SaveHmoTariffCommand, HmoTariffDto>
{
    public Task<HmoTariffDto> Handle(SaveHmoTariffCommand request, CancellationToken cancellationToken) =>
        hmo.SaveTariffAsync(request.HmoCompanyId, request.Request, cancellationToken);
}

public sealed class SaveHmoTariffCommandValidator : AbstractValidator<SaveHmoTariffCommand>
{
    public SaveHmoTariffCommandValidator()
    {
        RuleFor(x => x.HmoCompanyId).NotEmpty();
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.Items).NotEmpty();
        RuleForEach(x => x.Request.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.Code).NotEmpty().MaximumLength(64);
            item.RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
            item.RuleFor(x => x.CoveragePercent).InclusiveBetween(0m, 100m);
        });
    }
}
