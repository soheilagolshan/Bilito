using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Bilito.Identity.Application.Abstractions;
using Bilito.Identity.Application.Configuration;
using Bilito.Identity.Domain.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Bilito.Identity.Infrastructure.Authentication;

public sealed class JwtAccessTokenService(IOptions<JwtOptions> options) : IAccessTokenService
{
    public AccessTokenResult Create(User user, DateTimeOffset now)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.Issuer) || string.IsNullOrWhiteSpace(settings.Audience) ||
            string.IsNullOrWhiteSpace(settings.SigningKey))
        {
            throw new InvalidOperationException("JWT issuer, audience, and signing key must be configured.");
        }

        var signingKeyBytes = Encoding.UTF8.GetBytes(settings.SigningKey);
        if (signingKeyBytes.Length < 32)
        {
            throw new InvalidOperationException("JWT signing key must be at least 32 bytes long.");
        }

        var expiresAt = now.AddMinutes(settings.AccessTokenLifetimeMinutes);
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(signingKeyBytes),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: [new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString())],
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new AccessTokenResult(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}

public sealed class SecureRefreshTokenGenerator : IRefreshTokenGenerator
{
    public string Generate() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    public string Hash(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
