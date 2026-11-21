using FluentValidation;
using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Application.Services;
using Hms.Domain.Enums;
using MediatR;

namespace Hms.Application.Features.Billing;

/// <summary>Finance final approval; HMO bills must already be HMO-approved and co-pay paid.</summary>
public sealed record ApproveBillCommand(Guid BillId, ApproveBillRequest Request) : IRequest<BillDto>;

public sealed class ApproveBillCommandHandler(IBillingService billing) : IRequestHandler<ApproveBillCommand, BillDto>
{
    public async Task<BillDto> Handle(ApproveBillCommand request, CancellationToken cancellationToken)
    {
        await billing.ApproveAsync(request.BillId, request.Request.Notes, cancellationToken);
        return await billing.GetAsync(request.BillId, cancellationToken);
    }
}

public sealed class ApproveBillCommandValidator : AbstractValidator<ApproveBillCommand>
{
    public ApproveBillCommandValidator()
    {
        RuleFor(x => x.BillId).NotEmpty();
        RuleFor(x => x.Request.Notes).MaximumLength(2000);
    }
}
