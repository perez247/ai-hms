using FluentValidation;
using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Hmo;

/// <summary>HMO Office approval; Finance approval is still required before the bill is payable.</summary>
public sealed record ApproveHmoBillCommand(Guid BillId) : IRequest<BillDto>;

public sealed class ApproveHmoBillCommandHandler(IHmoService hmo, IBillingService billing)
    : IRequestHandler<ApproveHmoBillCommand, BillDto>
{
    public async Task<BillDto> Handle(ApproveHmoBillCommand request, CancellationToken cancellationToken)
    {
        await hmo.ApproveAsync(request.BillId, cancellationToken);
        return await billing.GetAsync(request.BillId, cancellationToken);
    }
}

public sealed class ApproveHmoBillCommandValidator : AbstractValidator<ApproveHmoBillCommand>
{
    public ApproveHmoBillCommandValidator() => RuleFor(x => x.BillId).NotEmpty();
}
