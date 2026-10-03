using Bilito.Identity.Application.Abstractions;
using Bilito.Identity.Application.Configuration;
using Bilito.Identity.Contracts.Authentication;
using Bilito.Identity.Domain.Authentication;
using Bilito.Identity.Domain.Users;

namespace Bilito.Identity.Application.Authentication;

public sealed class IdentityAuthenticationService(
    IIdentityStore store,
    IMobileNumberNormalizer mobileNumberNormalizer,
    IOtpCodeGenerator otpCodeGenerator,
    IOtpCodeHasher otpCodeHasher,
    IOtpDelivery otpDelivery,
    IAccessTokenService accessTokenService,
    IRefreshTokenGenerator refreshTokenGenerator,
    OtpOptions otpOptions,
    RefreshTokenOptions refreshTokenOptions,
    TimeProvider timeProvider)
{
    public async Task<RequestOtpResponse> RequestOtpAsync(
        RequestOtpRequest request,
        CancellationToken cancellationToken)
    {
        var mobile = mobileNumberNormalizer.Normalize(request.Mobile);
        var options = otpOptions;
        var now = timeProvider.GetUtcNow();
        var current = await store.FindLatestActiveOtpAsync(mobile, OtpPurpose.Authentication, cancellationToken);

        if (current is not null && current.CreatedAt.AddSeconds(options.ResendCooldownSeconds) > now)
        {
            throw new IdentityConflictException("Please wait before requesting another code.");
        }

        if (options.Length != 6 || options.LifetimeSeconds <= 0 || options.MaxAttempts <= 0)
        {
            throw new InvalidOperationException("OTP configuration is invalid.");
        }

        var challengeId = Guid.NewGuid();
        var code = otpCodeGenerator.Generate(options.Length);
        var challenge = OtpChallenge.Create(
            challengeId,
            mobile,
            otpCodeHasher.Hash(challengeId, mobile, code),
            OtpPurpose.Authentication,
            now,
            now.AddSeconds(options.LifetimeSeconds));

        var developmentOtp = await otpDelivery.DeliverAsync(mobile, code, cancellationToken);
        await store.ReplaceOtpAsync(challenge, now, cancellationToken);

        return new RequestOtpResponse(options.LifetimeSeconds, options.ResendCooldownSeconds, developmentOtp);
    }

    public async Task<AuthenticationResult> VerifyOtpAsync(
        VerifyOtpRequest request,
        CancellationToken cancellationToken)
    {
        var mobile = mobileNumberNormalizer.Normalize(request.Mobile);
        var options = otpOptions;
        var now = timeProvider.GetUtcNow();
        if (request.Otp.Length != options.Length || request.Otp.Any(character => character is < '0' or > '9'))
        {
            throw new IdentityUnauthorizedException("The verification code is invalid or expired.");
        }

        var challenge = await store.FindLatestActiveOtpAsync(mobile, OtpPurpose.Authentication, cancellationToken);

        if (challenge is null || challenge.ExpiresAt <= now || challenge.ConsumedAt is not null ||
            challenge.FailedAttempts >= options.MaxAttempts)
        {
            throw new IdentityUnauthorizedException("The verification code is invalid or expired.");
        }

        if (!otpCodeHasher.Verify(challenge.Id, mobile, request.Otp, challenge.CodeHash))
        {
            await store.RecordOtpFailureAsync(challenge.Id, now, options.MaxAttempts, cancellationToken);
            throw new IdentityUnauthorizedException("The verification code is invalid or expired.");
        }

        if (!await store.TryConsumeOtpAsync(challenge.Id, now, options.MaxAttempts, cancellationToken))
        {
            throw new IdentityUnauthorizedException("The verification code is invalid or expired.");
        }

        var user = await store.GetOrCreateUserAsync(mobile, now, cancellationToken);
        if (user.Status == UserStatus.Blocked)
        {
            throw new IdentityUnauthorizedException("This user cannot authenticate.");
        }

        user.RecordLogin(now);
        await store.UpdateUserAsync(user, cancellationToken);

        return await CreateAuthenticationResultAsync(user, now, cancellationToken);
    }

    public async Task<AuthenticationResult> RefreshAsync(
        string rawRefreshToken,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var current = await store.FindRefreshTokenAsync(
            refreshTokenGenerator.Hash(rawRefreshToken), cancellationToken);

        if (current.Token is null || current.User is null || current.User.Status == UserStatus.Blocked ||
            current.Token.ExpiresAt <= now || current.Token.RevokedAt is not null)
        {
            throw new IdentityUnauthorizedException("The refresh token is invalid.");
        }

        var replacementRaw = refreshTokenGenerator.Generate();
        var replacement = Bilito.Identity.Domain.Authentication.RefreshToken.Create(
            Guid.NewGuid(),
            current.User.Id,
            refreshTokenGenerator.Hash(replacementRaw),
            now,
            now.AddDays(refreshTokenOptions.LifetimeDays));

        if (!await store.RotateRefreshTokenAsync(current.Token, replacement, now, cancellationToken))
        {
            throw new IdentityUnauthorizedException("The refresh token is invalid.");
        }

        var accessToken = accessTokenService.Create(current.User, now);
        return new AuthenticationResult(accessToken, replacementRaw);
    }

    public async Task LogoutAsync(string? rawRefreshToken, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            await store.RevokeRefreshTokenAsync(
                refreshTokenGenerator.Hash(rawRefreshToken),
                timeProvider.GetUtcNow(),
                cancellationToken);
        }
    }

    private async Task<AuthenticationResult> CreateAuthenticationResultAsync(
        Bilito.Identity.Domain.Users.User user,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var accessToken = accessTokenService.Create(user, now);
        var rawRefreshToken = refreshTokenGenerator.Generate();
        var refreshToken = Bilito.Identity.Domain.Authentication.RefreshToken.Create(
            Guid.NewGuid(),
            user.Id,
            refreshTokenGenerator.Hash(rawRefreshToken),
            now,
            now.AddDays(refreshTokenOptions.LifetimeDays));

        await store.AddRefreshTokenAsync(refreshToken, cancellationToken);
        return new AuthenticationResult(accessToken, rawRefreshToken);
    }
}

public sealed record AuthenticationResult(AccessTokenResult AccessToken, string RefreshToken);
