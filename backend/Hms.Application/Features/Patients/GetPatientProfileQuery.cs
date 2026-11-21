using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Patients;

/// <summary>Consolidated patient profile; section visibility is applied inside the application service.</summary>
public sealed record GetPatientProfileQuery(Guid PatientId, int VitalsPage, int VitalsPageSize) : IRequest<PatientProfileDto>;

public sealed class GetPatientProfileQueryHandler(IPatientProfileService profiles)
    : IRequestHandler<GetPatientProfileQuery, PatientProfileDto>
{
    public Task<PatientProfileDto> Handle(GetPatientProfileQuery request, CancellationToken cancellationToken) =>
        profiles.GetAsync(request.PatientId, request.VitalsPage, request.VitalsPageSize, cancellationToken);
}

public sealed class GetPatientProfileQueryValidator : AbstractValidator<GetPatientProfileQuery>
{
    public GetPatientProfileQueryValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.VitalsPage).GreaterThanOrEqualTo(1);
        RuleFor(x => x.VitalsPageSize).InclusiveBetween(1, 50);
    }
}
