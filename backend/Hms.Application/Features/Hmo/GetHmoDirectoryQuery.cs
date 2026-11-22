using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Hmo;

public sealed record GetHmoDirectoryQuery : IRequest<IReadOnlyList<HmoDirectoryEntryDto>>;

public sealed class GetHmoDirectoryQueryHandler(IHmoService hmo) : IRequestHandler<GetHmoDirectoryQuery, IReadOnlyList<HmoDirectoryEntryDto>>
{
    public Task<IReadOnlyList<HmoDirectoryEntryDto>> Handle(GetHmoDirectoryQuery request, CancellationToken cancellationToken) =>
        hmo.GetDirectoryAsync(cancellationToken);
}
