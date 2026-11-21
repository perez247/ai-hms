using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Pharmacy;

public sealed record ConfirmTariffCommand(Guid BillId, ConfirmTariffRequest Request) : IRequest<Unit>;

public sealed class ConfirmTariffCommandHandler(IPharmacyService pharmacy) : IRequestHandler<ConfirmTariffCommand, Unit>
{
    public async Task<Unit> Handle(ConfirmTariffCommand request, CancellationToken cancellationToken)
    {
        await pharmacy.ConfirmTariffAsync(request.BillId, request.Request, cancellationToken);
        return Unit.Value;
    }
}

public sealed class ConfirmTariffCommandValidator : AbstractValidator<ConfirmTariffCommand>
{
    public ConfirmTariffCommandValidator()
    {
        RuleFor(x => x.BillId).NotEmpty();
        RuleFor(x => x.Request.BillLineIds).NotEmpty();
        RuleForEach(x => x.Request.BillLineIds).NotEmpty();
    }
}
