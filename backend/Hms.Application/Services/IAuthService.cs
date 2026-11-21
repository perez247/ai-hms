using Hms.Application.Common;
using Hms.Application.DTOs;
using Hms.Domain.Entities;
using Hms.Domain.Enums;

namespace Hms.Application.Services;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, string? ip, string? userAgent, CancellationToken cancellationToken = default);
    Task<AuthResponse> RefreshAsync(string refreshToken, string? ip, string? userAgent, CancellationToken cancellationToken = default);
}
