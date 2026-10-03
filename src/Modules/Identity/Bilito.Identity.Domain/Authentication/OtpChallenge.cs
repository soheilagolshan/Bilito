namespace Bilito.Identity.Domain.Authentication;

public sealed class OtpChallenge
{
    private OtpChallenge()
    {
    }

    private OtpChallenge(
        Guid id,
        string mobile,
        string codeHash,
        OtpPurpose purpose,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt)
    {
        Id = id;
        Mobile = mobile;
        CodeHash = codeHash;
        Purpose = purpose;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    public Guid Id { get; private set; }
    public string Mobile { get; private set; } = string.Empty;
    public string CodeHash { get; private set; } = string.Empty;
    public OtpPurpose Purpose { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? ConsumedAt { get; private set; }
    public int FailedAttempts { get; private set; }

    public static OtpChallenge Create(
        Guid id,
        string mobile,
        string codeHash,
        OtpPurpose purpose,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt) =>
        new(id, mobile, codeHash, purpose, createdAt, expiresAt);
}
