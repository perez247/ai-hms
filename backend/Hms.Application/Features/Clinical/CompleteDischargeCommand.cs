using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Clinical;

public sealed record CompleteDischargeCommand(Guid AdmissionId) : IRequest<AdmissionDto>;

public sealed class CompleteDischargeCommandHandler(IClinicalService clinical)
    : IRequestHandler<CompleteDischargeCommand, AdmissionDto>
{
    public Task<AdmissionDto> Handle(CompleteDischargeCommand request, CancellationToken cancellationToken) =>
        clinical.CompleteDischargeAsync(request.AdmissionId, cancellationToken);
}

public sealed class CompleteDischargeCommandValidator : AbstractValidator<CompleteDischargeCommand>
{
    public CompleteDischargeCommandValidator() => RuleFor(x => x.AdmissionId).NotEmpty();
}
