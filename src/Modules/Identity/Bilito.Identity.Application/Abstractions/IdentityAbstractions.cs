using Bilito.Identity.Domain.Authentication;
using Bilito.Identity.Domain.Users;

namespace Bilito.Identity.Application.Abstractions;

public interface IMobileNumberNormalizer
{
    string Normalize(string mobile);
}

public interface IOtpCodeGenerator
{
    string Generate(int length);
}

public interface IOtpCodeHasher
{
    string Hash(Guid challengeId, string mobile, string code);
    bool Verify(Guid challengeId, string mobile, string code, string hash);
}

public interface IOtpDelivery
{
    Task<string?> DeliverAsync(string mobile, string code, CancellationToken cancellationToken);
}

public interface IAccessTokenService
{
    AccessTokenResult Create(User user, DateTimeOffset now);
}

public sealed record AccessTokenResult(string Token, DateTimeOffset ExpiresAt);

public interface IRefreshTokenGenerator
{
    string Generate();
    string Hash(string rawToken);
}

public interface IIdentityStore
{
    Task<OtpChallenge?> FindLatestActiveOtpAsync(string mobile, OtpPurpose purpose, CancellationToken cancellationToken);
    Task ReplaceOtpAsync(OtpChallenge challenge, DateTimeOffset now, CancellationToken cancellationToken);
    Task<bool> TryConsumeOtpAsync(Guid challengeId, DateTimeOffset now, int maxAttempts, CancellationToken cancellationToken);
    Task<bool> RecordOtpFailureAsync(Guid challengeId, DateTimeOffset now, int maxAttempts, CancellationToken cancellationToken);
    Task<User> GetOrCreateUserAsync(string mobile, DateTimeOffset now, CancellationToken cancellationToken);
    Task UpdateUserAsync(User user, CancellationToken cancellationToken);
    Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken);
    Task<(RefreshToken? Token, User? User)> FindRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken);
    Task<bool> RotateRefreshTokenAsync(RefreshToken current, RefreshToken replacement, DateTimeOffset now, CancellationToken cancellationToken);
    Task RevokeRefreshTokenAsync(string tokenHash, DateTimeOffset now, CancellationToken cancellationToken);
    Task<User?> FindUserAsync(Guid userId, CancellationToken cancellationToken);
}

public sealed class IdentityValidationException(string message) : Exception(message);

public sealed class IdentityConflictException(string message) : Exception(message);

public sealed class IdentityUnauthorizedException(string message) : Exception(message);
