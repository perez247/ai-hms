using System.Security.Claims;
using Hms.Application.Common;
using Hms.Domain.Enums;

namespace Hms.Api.Middleware;

/// <summary>Adapts HttpContext claims to the Application ICurrentUser port.</summary>
public sealed class HttpContextCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private HttpContext? Context => accessor.HttpContext;

    public Guid? Id
    {
        get
        {
            var value = Context?.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Context?.User.FindFirstValue("sub");
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public StaffRole? Role
    {
        get
        {
            var value = Context?.User.FindFirstValue("staff_role") ?? Context?.User.FindFirstValue(ClaimTypes.Role);
            return Enum.TryParse<StaffRole>(value, out var role) ? role : null;
        }
    }

    public string? FullName => Context?.User.FindFirstValue("full_name") ?? Context?.User.Identity?.Name;
    public bool IsAuthenticated => Context?.User.Identity?.IsAuthenticated == true;
    public string? IpAddress => Context?.Connection.RemoteIpAddress?.ToString();
    public string? UserAgent => Context?.Request.Headers.UserAgent.ToString();

    public Guid RequireId() => Id ?? throw new AppUnauthorizedException("Authenticated user is required.");
}
