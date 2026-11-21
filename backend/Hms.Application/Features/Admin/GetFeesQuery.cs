using System.Text.Json;
using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Admin;

public sealed record GetFeesQuery : IRequest<IReadOnlyList<FeeDto>>;

public sealed class GetFeesQueryHandler(IAdminService admin) : IRequestHandler<GetFeesQuery, IReadOnlyList<FeeDto>>
{
    public Task<IReadOnlyList<FeeDto>> Handle(GetFeesQuery request, CancellationToken cancellationToken) =>
        admin.GetFeesAsync(cancellationToken);
}
