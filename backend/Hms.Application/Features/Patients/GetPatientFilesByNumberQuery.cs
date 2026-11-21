using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Patients;

public sealed record GetPatientFilesByNumberQuery(string PatientNumber) : IRequest<IReadOnlyList<PatientFileDto>>;

public sealed class GetPatientFilesByNumberQueryHandler(IPatientProfileService profiles)
    : IRequestHandler<GetPatientFilesByNumberQuery, IReadOnlyList<PatientFileDto>>
{
    public Task<IReadOnlyList<PatientFileDto>> Handle(GetPatientFilesByNumberQuery request, CancellationToken cancellationToken) =>
        profiles.GetFilesByNumberAsync(request.PatientNumber, cancellationToken);
}

public sealed class GetPatientFilesByNumberQueryValidator : AbstractValidator<GetPatientFilesByNumberQuery>
{
    public GetPatientFilesByNumberQueryValidator() => RuleFor(x => x.PatientNumber).NotEmpty().MaximumLength(50);
}
