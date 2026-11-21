using FluentValidation;
using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Appointments;

public sealed record SearchAppointmentsQuery(DateOnly? Date, Guid? DoctorId, int Page, int PageSize) : IRequest<PagedResult<AppointmentDto>>;

public sealed class SearchAppointmentsQueryHandler(IAppointmentService appointments)
    : IRequestHandler<SearchAppointmentsQuery, PagedResult<AppointmentDto>>
{
    public Task<PagedResult<AppointmentDto>> Handle(SearchAppointmentsQuery request, CancellationToken cancellationToken) =>
        appointments.SearchAsync(request.Date, request.DoctorId, request.Page, request.PageSize, cancellationToken);
}

public sealed class SearchAppointmentsQueryValidator : AbstractValidator<SearchAppointmentsQuery>
{
    public SearchAppointmentsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PageSize).GreaterThanOrEqualTo(0);
    }
}
