using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Patients;

public sealed record GetPatientFinanceByNumberQuery(string PatientNumber) : IRequest<PatientFinanceDto?>;

public sealed class GetPatientFinanceByNumberQueryHandler(IPatientProfileService profiles)
    : IRequestHandler<GetPatientFinanceByNumberQuery, PatientFinanceDto?>
{
    public Task<PatientFinanceDto?> Handle(GetPatientFinanceByNumberQuery request, CancellationToken cancellationToken) =>
        profiles.GetFinanceByNumberAsync(request.PatientNumber, cancellationToken);
}

public sealed class GetPatientFinanceByNumberQueryValidator : AbstractValidator<GetPatientFinanceByNumberQuery>
{
    public GetPatientFinanceByNumberQueryValidator() => RuleFor(x => x.PatientNumber).NotEmpty().MaximumLength(50);
}
