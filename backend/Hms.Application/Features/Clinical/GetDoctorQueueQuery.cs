using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Clinical;

public sealed record GetDoctorQueueQuery : IRequest<DoctorQueueDto>;

public sealed class GetDoctorQueueQueryHandler(IClinicalService clinical) : IRequestHandler<GetDoctorQueueQuery, DoctorQueueDto>
{
    public Task<DoctorQueueDto> Handle(GetDoctorQueueQuery request, CancellationToken cancellationToken) =>
        clinical.GetDoctorQueueAsync(cancellationToken);
}
