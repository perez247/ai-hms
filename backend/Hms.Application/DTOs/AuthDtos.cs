using Hms.Domain.Enums;

namespace Hms.Application.DTOs;

public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshRequest(string RefreshToken);
public sealed record AuthResponse(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt, MeResponse User);
public sealed record MeResponse(Guid Id, string FullName, StaffRole Role, string RoleName, IReadOnlyList<string> Permissions);
