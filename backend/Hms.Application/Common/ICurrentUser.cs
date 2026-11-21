using Hms.Domain.Enums;

namespace Hms.Application.Common;

/// <summary>Request-scoped identity abstraction; the API supplies the HttpContext-backed implementation.</summary>
public interface ICurrentUser
{
    Guid? Id { get; }
    StaffRole? Role { get; }
    string? FullName { get; }
    bool IsAuthenticated { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
    Guid RequireId();
}
