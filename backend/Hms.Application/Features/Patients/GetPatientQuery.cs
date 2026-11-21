using FluentValidation;
using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Patients;

public sealed record GetPatientQuery(Guid Id) : IRequest<PatientDto>;

public sealed class GetPatientQueryHandler(IPatientService patients) : IRequestHandler<GetPatientQuery, PatientDto>
{
    public Task<PatientDto> Handle(GetPatientQuery request, CancellationToken cancellationToken) =>
        patients.GetAsync(request.Id, cancellationToken);
}

public sealed class GetPatientQueryValidator : AbstractValidator<GetPatientQuery>
{
    public GetPatientQueryValidator() => RuleFor(x => x.Id).NotEmpty();
}
