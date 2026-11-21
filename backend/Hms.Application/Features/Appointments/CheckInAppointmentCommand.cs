using FluentValidation;
using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Appointments;

public sealed record CheckInAppointmentCommand(Guid AppointmentId) : IRequest<AppointmentDto>;

public sealed class CheckInAppointmentCommandHandler(IAppointmentService appointments)
    : IRequestHandler<CheckInAppointmentCommand, AppointmentDto>
{
    public Task<AppointmentDto> Handle(CheckInAppointmentCommand request, CancellationToken cancellationToken) =>
        appointments.CheckInAsync(request.AppointmentId, cancellationToken);
}

public sealed class CheckInAppointmentCommandValidator : AbstractValidator<CheckInAppointmentCommand>
{
    public CheckInAppointmentCommandValidator() => RuleFor(x => x.AppointmentId).NotEmpty();
}
