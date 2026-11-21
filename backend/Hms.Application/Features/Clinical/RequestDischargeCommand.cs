using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Clinical;

/// <summary>Generates bed-day charges, commits the final admission bill and moves the admission to PendingDischarge.</summary>
public sealed record RequestDischargeCommand(RequestDischargeRequest Request) : IRequest<DischargeRequestResult>;

public sealed class RequestDischargeCommandHandler(IClinicalService clinical)
    : IRequestHandler<RequestDischargeCommand, DischargeRequestResult>
{
    public Task<DischargeRequestResult> Handle(RequestDischargeCommand request, CancellationToken cancellationToken) =>
        clinical.RequestDischargeAsync(request.Request, cancellationToken);
}

public sealed class RequestDischargeCommandValidator : AbstractValidator<RequestDischargeCommand>
{
    public RequestDischargeCommandValidator()
    {
        RuleFor(x => x.Request.AdmissionId).NotEmpty();
        RuleFor(x => x.Request.DischargeSummary).NotEmpty().MaximumLength(20000);
    }
}
