using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Patients;

public sealed record GetPatientHmosByNumberQuery(string PatientNumber) : IRequest<IReadOnlyList<PatientHmoEnrollmentDto>>;

public sealed class GetPatientHmosByNumberQueryHandler(IPatientProfileService profiles)
    : IRequestHandler<GetPatientHmosByNumberQuery, IReadOnlyList<PatientHmoEnrollmentDto>>
{
    public Task<IReadOnlyList<PatientHmoEnrollmentDto>> Handle(GetPatientHmosByNumberQuery request, CancellationToken cancellationToken) =>
        profiles.GetHmosByNumberAsync(request.PatientNumber, cancellationToken);
}

public sealed class GetPatientHmosByNumberQueryValidator : AbstractValidator<GetPatientHmosByNumberQuery>
{
    public GetPatientHmosByNumberQueryValidator() => RuleFor(x => x.PatientNumber).NotEmpty().MaximumLength(50);
}
