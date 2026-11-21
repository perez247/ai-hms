using Hms.Application.Common;
using Hms.Infrastructure.Persistence;
using Hms.Application.DTOs;
using Hms.Application.Security;
using Hms.Application.Services;
using Hms.Infrastructure.Identity;
using Hms.Infrastructure.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Hms.Infrastructure.Services;

/// <summary>Login and refresh-token rotation; issues JWT claims and hashed refresh tokens.</summary>
public sealed class AuthService(
    UserManager<AppUser> userManager,
    HmsDbContext db,
    JwtTokenService tokenService,
    IOptions<JwtOptions> options) : IAuthService
{
    private readonly JwtOptions _options = options.Value;

    /// <summary>Validates credentials and issues a new access/refresh pair. Disabled accounts are rejected.</summary>
    public async Task<AuthResponse> LoginAsync(LoginRequest request, string? ip, string? userAgent, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || !user.IsActive || !await userManager.CheckPasswordAsync(user, request.Password))
            throw new AppUnauthorizedException("Invalid email or password, or the account is disabled.");

        var (response, token) = Issue(user, ip, userAgent);
        db.RefreshTokens.Add(token);
        await db.SaveChangesAsync(cancellationToken);
        return response;
    }

    /// <summary>Rotates a valid refresh token; the old token is revoked and linked to its replacement.</summary>
    public async Task<AuthResponse> RefreshAsync(string refreshToken, string? ip, string? userAgent, CancellationToken cancellationToken = default)
    {
        var hash = JwtTokenService.Hash(refreshToken);
        var existing = await db.RefreshTokens
            .Include(x => x.User)
            .SingleOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);

        if (existing is null || existing.RevokedAt is not null || existing.ExpiresAt <= DateTimeOffset.UtcNow || existing.User is null || !existing.User.IsActive)
            throw new AppUnauthorizedException("Refresh token is invalid or expired.");

        var (response, replacement) = Issue(existing.User, ip, userAgent);
        existing.RevokedAt = DateTimeOffset.UtcNow;
        existing.ReplacedByTokenId = replacement.Id;
        db.RefreshTokens.Add(replacement);
        await db.SaveChangesAsync(cancellationToken);
        return response;
    }

    private (AuthResponse Response, RefreshToken Token) Issue(AppUser user, string? ip, string? userAgent)
    {
        var (accessToken, expiresAt) = tokenService.CreateAccessToken(user);
        var refresh = JwtTokenService.CreateRefreshToken();
        var token = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = JwtTokenService.Hash(refresh),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(_options.RefreshTokenDays),
            CreatedByIp = ip,
            UserAgent = userAgent
        };

        var me = new MeResponse(user.Id, user.FullName, user.StaffRole, user.StaffRole.ToString(), PermissionCatalog.ForRole(user.StaffRole));
        return (new AuthResponse(accessToken, refresh, expiresAt, me), token);
    }
}
