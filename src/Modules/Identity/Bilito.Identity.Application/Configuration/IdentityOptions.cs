namespace Bilito.Identity.Application.Configuration;

public sealed class OtpOptions
{
    public int Length { get; init; } = 6;
    public int LifetimeSeconds { get; init; } = 120;
    public int MaxAttempts { get; init; } = 5;
    public int ResendCooldownSeconds { get; init; } = 60;
    public bool ExposeDevelopmentOtp { get; init; }
}

public sealed class JwtOptions
{
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenLifetimeMinutes { get; init; } = 15;
}

public sealed class RefreshTokenOptions
{
    public int LifetimeDays { get; init; } = 30;
    public string CookieName { get; init; } = "bilito_refresh_token";
    public bool SecureCookie { get; init; } = true;
}
