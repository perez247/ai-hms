using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Patients;

public sealed record GetPatientFitnessByNumberQuery(string PatientNumber) : IRequest<IReadOnlyList<FitnessCertificateDto>>;

public sealed class GetPatientFitnessByNumberQueryHandler(IPatientProfileService profiles)
    : IRequestHandler<GetPatientFitnessByNumberQuery, IReadOnlyList<FitnessCertificateDto>>
{
    public Task<IReadOnlyList<FitnessCertificateDto>> Handle(GetPatientFitnessByNumberQuery request, CancellationToken cancellationToken) =>
        profiles.GetFitnessByNumberAsync(request.PatientNumber, cancellationToken);
}

public sealed class GetPatientFitnessByNumberQueryValidator : AbstractValidator<GetPatientFitnessByNumberQuery>
{
    public GetPatientFitnessByNumberQueryValidator() => RuleFor(x => x.PatientNumber).NotEmpty().MaximumLength(50);
}
