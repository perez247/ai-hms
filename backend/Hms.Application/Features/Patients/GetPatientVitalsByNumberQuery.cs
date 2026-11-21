using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Patients;

public sealed record GetPatientVitalsByNumberQuery(string PatientNumber, int Page, int PageSize) : IRequest<PatientVitalsPageDto>;

public sealed class GetPatientVitalsByNumberQueryHandler(IPatientProfileService profiles)
    : IRequestHandler<GetPatientVitalsByNumberQuery, PatientVitalsPageDto>
{
    public Task<PatientVitalsPageDto> Handle(GetPatientVitalsByNumberQuery request, CancellationToken cancellationToken) =>
        profiles.GetVitalsByNumberAsync(request.PatientNumber, request.Page, request.PageSize, cancellationToken);
}

public sealed class GetPatientVitalsByNumberQueryValidator : AbstractValidator<GetPatientVitalsByNumberQuery>
{
    public GetPatientVitalsByNumberQueryValidator()
    {
        RuleFor(x => x.PatientNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
    }
}
