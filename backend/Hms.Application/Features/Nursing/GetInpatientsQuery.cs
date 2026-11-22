using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Nursing;

public sealed record GetInpatientsQuery : IRequest<IReadOnlyList<NurseInpatientDto>>;

public sealed class GetInpatientsQueryHandler(INurseService nurse)
    : IRequestHandler<GetInpatientsQuery, IReadOnlyList<NurseInpatientDto>>
{
    public Task<IReadOnlyList<NurseInpatientDto>> Handle(GetInpatientsQuery request, CancellationToken cancellationToken) =>
        nurse.GetInpatientsAsync(cancellationToken);
}
