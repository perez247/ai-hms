using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Clinical;

public sealed record GetAdmissionQuery(Guid AdmissionId) : IRequest<AdmissionDto>;

public sealed class GetAdmissionQueryHandler(IClinicalService clinical) : IRequestHandler<GetAdmissionQuery, AdmissionDto>
{
    public Task<AdmissionDto> Handle(GetAdmissionQuery request, CancellationToken cancellationToken) =>
        clinical.GetAdmissionAsync(request.AdmissionId, cancellationToken);
}

public sealed class GetAdmissionQueryValidator : AbstractValidator<GetAdmissionQuery>
{
    public GetAdmissionQueryValidator() => RuleFor(x => x.AdmissionId).NotEmpty();
}
