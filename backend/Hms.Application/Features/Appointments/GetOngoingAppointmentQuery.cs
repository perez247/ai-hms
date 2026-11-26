using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Appointments;

public sealed record GetOngoingAppointmentQuery(string PatientNumber) : IRequest<OngoingAppointmentDto?>;

public sealed class GetOngoingAppointmentQueryHandler(IAppointmentService appointments)
    : IRequestHandler<GetOngoingAppointmentQuery, OngoingAppointmentDto?>
{
    public Task<OngoingAppointmentDto?> Handle(GetOngoingAppointmentQuery request, CancellationToken cancellationToken) =>
        appointments.GetOngoingForPatientAsync(request.PatientNumber, cancellationToken);
}

public sealed class GetOngoingAppointmentQueryValidator : AbstractValidator<GetOngoingAppointmentQuery>
{
    public GetOngoingAppointmentQueryValidator() => RuleFor(x => x.PatientNumber).NotEmpty().MaximumLength(50);
}
