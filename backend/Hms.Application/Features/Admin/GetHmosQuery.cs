using System.Text.Json;
using FluentValidation;
using Hms.Application.DTOs;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Admin;

public sealed record GetHmosQuery : IRequest<IReadOnlyList<HmoCompanyDto>>;

public sealed class GetHmosQueryHandler(IAdminService admin) : IRequestHandler<GetHmosQuery, IReadOnlyList<HmoCompanyDto>>
{
    public Task<IReadOnlyList<HmoCompanyDto>> Handle(GetHmosQuery request, CancellationToken cancellationToken) =>
        admin.GetHmosAsync(cancellationToken);
}
