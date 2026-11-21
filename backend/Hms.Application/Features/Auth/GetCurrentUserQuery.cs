using FluentValidation;
using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Application.Security;
using Hms.Application.Services;
using MediatR;

namespace Hms.Application.Features.Auth;

public sealed record GetCurrentUserQuery : IRequest<MeResponse>;

public sealed class GetCurrentUserQueryHandler(ICurrentUser currentUser) : IRequestHandler<GetCurrentUserQuery, MeResponse>
{
    public Task<MeResponse> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var id = currentUser.RequireId();
        var role = currentUser.Role ?? throw new AppUnauthorizedException("Authenticated role is required.");
        return Task.FromResult(new MeResponse(id, currentUser.FullName ?? "User", role, role.ToString(), PermissionCatalog.ForRole(role)));
    }
}
