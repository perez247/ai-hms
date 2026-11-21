using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Patients;

public sealed record GetPatientVitalsQuery(Guid PatientId, int Page, int PageSize) : IRequest<PatientVitalsPageDto>;

public sealed class GetPatientVitalsQueryHandler(IPatientProfileService profiles)
    : IRequestHandler<GetPatientVitalsQuery, PatientVitalsPageDto>
{
    public Task<PatientVitalsPageDto> Handle(GetPatientVitalsQuery request, CancellationToken cancellationToken) =>
        profiles.GetVitalsAsync(request.PatientId, request.Page, request.PageSize, cancellationToken);
}

public sealed class GetPatientVitalsQueryValidator : AbstractValidator<GetPatientVitalsQuery>
{
    public GetPatientVitalsQueryValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
    }
}
