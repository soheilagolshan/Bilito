using Bilito.Identity.Application.Abstractions;
using Bilito.Identity.Domain.Authentication;
using Bilito.Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Bilito.Identity.Infrastructure.Persistence;

public sealed class IdentityStore(IdentityDbContext dbContext) : IIdentityStore
{
    public Task<OtpChallenge?> FindLatestActiveOtpAsync(
        string mobile,
        OtpPurpose purpose,
        CancellationToken cancellationToken) =>
        dbContext.OtpChallenges
            .Where(challenge => challenge.Mobile == mobile && challenge.Purpose == purpose && challenge.ConsumedAt == null)
            .OrderByDescending(challenge => challenge.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task ReplaceOtpAsync(
        OtpChallenge challenge,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.OtpChallenges
            .Where(existing => existing.Mobile == challenge.Mobile &&
                               existing.Purpose == challenge.Purpose &&
                               existing.ConsumedAt == null)
            .ExecuteUpdateAsync(update => update.SetProperty(existing => existing.ConsumedAt, now), cancellationToken);

        dbContext.OtpChallenges.Add(challenge);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<bool> TryConsumeOtpAsync(
        Guid challengeId,
        DateTimeOffset now,
        int maxAttempts,
        CancellationToken cancellationToken)
    {
        var updated = await dbContext.OtpChallenges
            .Where(challenge => challenge.Id == challengeId &&
                               challenge.ConsumedAt == null &&
                               challenge.ExpiresAt > now &&
                               challenge.FailedAttempts < maxAttempts)
            .ExecuteUpdateAsync(update => update.SetProperty(challenge => challenge.ConsumedAt, now), cancellationToken);

        return updated == 1;
    }

    public async Task<bool> RecordOtpFailureAsync(
        Guid challengeId,
        DateTimeOffset now,
        int maxAttempts,
        CancellationToken cancellationToken)
    {
        var updated = await dbContext.OtpChallenges
            .Where(challenge => challenge.Id == challengeId &&
                               challenge.ConsumedAt == null &&
                               challenge.ExpiresAt > now &&
                               challenge.FailedAttempts < maxAttempts)
            .ExecuteUpdateAsync(update => update.SetProperty(
                challenge => challenge.FailedAttempts,
                challenge => challenge.FailedAttempts + 1), cancellationToken);

        return updated == 1;
    }

    public async Task<User> GetOrCreateUserAsync(
        string mobile,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.Users.SingleOrDefaultAsync(user => user.Mobile == mobile, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var user = User.Create(Guid.NewGuid(), mobile, now);
        dbContext.Users.Add(user);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return user;
        }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();
            return await dbContext.Users.SingleAsync(existingUser => existingUser.Mobile == mobile, cancellationToken);
        }
    }

    public async Task UpdateUserAsync(User user, CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
    {
        dbContext.RefreshTokens.Add(refreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<(RefreshToken? Token, User? User)> FindRefreshTokenAsync(
        string tokenHash,
        CancellationToken cancellationToken)
    {
        var token = await dbContext.RefreshTokens.SingleOrDefaultAsync(
            refreshToken => refreshToken.TokenHash == tokenHash,
            cancellationToken);
        if (token is null)
        {
            return (null, null);
        }

        var user = await dbContext.Users.SingleOrDefaultAsync(user => user.Id == token.UserId, cancellationToken);
        return (token, user);
    }

    public async Task<bool> RotateRefreshTokenAsync(
        RefreshToken current,
        RefreshToken replacement,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var revoked = await dbContext.RefreshTokens
            .Where(token => token.Id == current.Id && token.RevokedAt == null && token.ExpiresAt > now)
            .ExecuteUpdateAsync(update => update
                .SetProperty(token => token.RevokedAt, now)
                .SetProperty(token => token.ReplacedByTokenId, replacement.Id), cancellationToken);

        if (revoked != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        dbContext.RefreshTokens.Add(replacement);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task RevokeRefreshTokenAsync(
        string tokenHash,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await dbContext.RefreshTokens
            .Where(token => token.TokenHash == tokenHash && token.RevokedAt == null)
            .ExecuteUpdateAsync(update => update.SetProperty(token => token.RevokedAt, now), cancellationToken);
    }

    public Task<User?> FindUserAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext.Users.SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);
}
