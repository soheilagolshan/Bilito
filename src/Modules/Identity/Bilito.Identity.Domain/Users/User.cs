namespace Bilito.Identity.Domain.Users;

public sealed class User
{
    private User()
    {
    }

    private User(Guid id, string mobile, DateTimeOffset createdAt)
    {
        Id = id;
        Mobile = mobile;
        MobileVerified = true;
        Status = UserStatus.Active;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public string Mobile { get; private set; } = string.Empty;
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? NationalCode { get; private set; }
    public string? Gender { get; private set; }
    public string? Avatar { get; private set; }
    public bool MobileVerified { get; private set; }
    public UserStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }

    public static User Create(Guid id, string mobile, DateTimeOffset createdAt) =>
        new(id, mobile, createdAt);

    public void RecordLogin(DateTimeOffset occurredAt)
    {
        LastLoginAt = occurredAt;
    }

    public void UpdateProfile(
        string? firstName,
        string? lastName,
        string? nationalCode,
        string? gender,
        string? avatar)
    {
        FirstName = Normalize(firstName);
        LastName = Normalize(lastName);
        NationalCode = Normalize(nationalCode);
        Gender = Normalize(gender);
        Avatar = string.IsNullOrWhiteSpace(avatar) ? Avatar : avatar;
    }

    public string DisplayName =>
        string.Join(" ", new[] { FirstName, LastName }.Where(value => !string.IsNullOrWhiteSpace(value))).Trim();

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
