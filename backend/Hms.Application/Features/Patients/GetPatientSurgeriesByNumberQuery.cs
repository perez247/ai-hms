using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Patients;

public sealed record GetPatientSurgeriesByNumberQuery(string PatientNumber) : IRequest<IReadOnlyList<PatientSurgeryDto>>;

public sealed class GetPatientSurgeriesByNumberQueryHandler(IPatientProfileService profiles)
    : IRequestHandler<GetPatientSurgeriesByNumberQuery, IReadOnlyList<PatientSurgeryDto>>
{
    public Task<IReadOnlyList<PatientSurgeryDto>> Handle(GetPatientSurgeriesByNumberQuery request, CancellationToken cancellationToken) =>
        profiles.GetSurgeriesByNumberAsync(request.PatientNumber, cancellationToken);
}

public sealed class GetPatientSurgeriesByNumberQueryValidator : AbstractValidator<GetPatientSurgeriesByNumberQuery>
{
    public GetPatientSurgeriesByNumberQueryValidator() => RuleFor(x => x.PatientNumber).NotEmpty().MaximumLength(50);
}
