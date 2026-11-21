using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Reference;

public sealed record GetPatientHmoEnrollmentsQuery(Guid PatientId) : IRequest<IReadOnlyList<HmoEnrollmentOptionDto>>;

public sealed class GetPatientHmoEnrollmentsQueryHandler(IReferenceService reference)
    : IRequestHandler<GetPatientHmoEnrollmentsQuery, IReadOnlyList<HmoEnrollmentOptionDto>>
{
    public Task<IReadOnlyList<HmoEnrollmentOptionDto>> Handle(GetPatientHmoEnrollmentsQuery request, CancellationToken cancellationToken) =>
        reference.GetPatientHmoEnrollmentsAsync(request.PatientId, cancellationToken);
}

public sealed class GetPatientHmoEnrollmentsQueryValidator : AbstractValidator<GetPatientHmoEnrollmentsQuery>
{
    public GetPatientHmoEnrollmentsQueryValidator() => RuleFor(x => x.PatientId).NotEmpty();
}
