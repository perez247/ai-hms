namespace Hms.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "Hms.Api";
    public string Audience { get; set; } = "Hms.Web";
    public string SigningKey { get; set; } = "replace-this-development-signing-key-with-at-least-32-chars";
    public int AccessTokenMinutes { get; set; } = 60;
    public int RefreshTokenDays { get; set; } = 7;
}
