using FluentValidation;
using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Hmo;

public sealed record UpdateHmoCoverageCommand(Guid BillId, UpdateHmoCoverageRequest Request) : IRequest<BillDto>;

public sealed class UpdateHmoCoverageCommandHandler(IHmoService hmo)
    : IRequestHandler<UpdateHmoCoverageCommand, BillDto>
{
    public Task<BillDto> Handle(UpdateHmoCoverageCommand request, CancellationToken cancellationToken) =>
        hmo.UpdateCoverageAsync(request.BillId, request.Request, cancellationToken);
}

public sealed class UpdateHmoCoverageCommandValidator : AbstractValidator<UpdateHmoCoverageCommand>
{
    public UpdateHmoCoverageCommandValidator()
    {
        RuleFor(x => x.BillId).NotEmpty();
        RuleForEach(x => x.Request.Lines).ChildRules(line =>
        {
            line.RuleFor(x => x.BillLineId).NotEmpty();
            line.RuleFor(x => x.HmoAmount).GreaterThanOrEqualTo(0);
        });
    }
}
