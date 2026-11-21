using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Patients;

public sealed record GetPatientBasicProfileQuery(string PatientNumber) : IRequest<PatientBasicProfileDto>;

public sealed class GetPatientBasicProfileQueryHandler(IPatientProfileService profiles)
    : IRequestHandler<GetPatientBasicProfileQuery, PatientBasicProfileDto>
{
    public Task<PatientBasicProfileDto> Handle(GetPatientBasicProfileQuery request, CancellationToken cancellationToken) =>
        profiles.GetBasicByNumberAsync(request.PatientNumber, cancellationToken);
}

public sealed class GetPatientBasicProfileQueryValidator : AbstractValidator<GetPatientBasicProfileQuery>
{
    public GetPatientBasicProfileQueryValidator() => RuleFor(x => x.PatientNumber).NotEmpty().MaximumLength(50);
}
