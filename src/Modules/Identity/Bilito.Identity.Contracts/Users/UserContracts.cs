namespace Bilito.Identity.Contracts.Users;

public sealed record CurrentUserResponse(
    Guid Id,
    string Mobile,
    bool MobileVerified,
    string? FirstName,
    string? LastName,
    string? NationalCode,
    string? Gender,
    string? Avatar)
{
    public string DisplayName => string.Join(" ", new[] { FirstName, LastName }
        .Where(value => !string.IsNullOrWhiteSpace(value))).Trim();
}

public sealed record UpdateUserProfileRequest(
    string? FirstName,
    string? LastName,
    string? NationalCode,
    string? Gender,
    string? Avatar);
