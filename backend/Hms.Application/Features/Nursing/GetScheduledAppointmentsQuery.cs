using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Nursing;

public sealed record GetScheduledAppointmentsQuery : IRequest<IReadOnlyList<NurseAppointmentDto>>;

public sealed class GetScheduledAppointmentsQueryHandler(INurseService nurse)
    : IRequestHandler<GetScheduledAppointmentsQuery, IReadOnlyList<NurseAppointmentDto>>
{
    public Task<IReadOnlyList<NurseAppointmentDto>> Handle(GetScheduledAppointmentsQuery request, CancellationToken cancellationToken) =>
        nurse.GetScheduledAppointmentsAsync(cancellationToken);
}
