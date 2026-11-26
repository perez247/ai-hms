using FluentValidation;
using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Appointments;

public sealed record CreateAppointmentCommand(CreateAppointmentRequest Request) : IRequest<AppointmentDto>;

public sealed class CreateAppointmentCommandHandler(IAppointmentService appointments)
    : IRequestHandler<CreateAppointmentCommand, AppointmentDto>
{
    public Task<AppointmentDto> Handle(CreateAppointmentCommand request, CancellationToken cancellationToken) =>
        appointments.CreateAsync(request.Request, cancellationToken);
}

public sealed class CreateAppointmentCommandValidator : AbstractValidator<CreateAppointmentCommand>
{
    public CreateAppointmentCommandValidator()
    {
        RuleFor(x => x.Request.PatientId).NotEmpty();
        RuleFor(x => x.Request.DoctorId).NotEmpty();
        RuleFor(x => x.Request.ScheduledAt).GreaterThan(DateTimeOffset.UtcNow);
        RuleFor(x => x.Request.DurationMinutes).GreaterThan(0);
        RuleFor(x => x.Request.Reason)
            .Must(Hms.Domain.Enums.AppointmentReasons.IsValid)
            .WithMessage("Reason must be Consultation or Emergency.");
    }
}
