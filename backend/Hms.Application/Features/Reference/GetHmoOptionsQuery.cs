using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Reference;

public sealed record GetHmoOptionsQuery : IRequest<IReadOnlyList<HmoOptionDto>>;

public sealed class GetHmoOptionsQueryHandler(IReferenceService reference) : IRequestHandler<GetHmoOptionsQuery, IReadOnlyList<HmoOptionDto>>
{
    public Task<IReadOnlyList<HmoOptionDto>> Handle(GetHmoOptionsQuery request, CancellationToken cancellationToken) =>
        reference.GetHmosAsync(cancellationToken);
}
