using Bilito.Identity.Application.Abstractions;
using Bilito.Identity.Contracts.Users;

namespace Bilito.Identity.Application.Users;

public sealed class CurrentUserService(IIdentityStore store)
{
    public async Task<CurrentUserResponse> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await store.FindUserAsync(userId, cancellationToken)
            ?? throw new IdentityUnauthorizedException("The authenticated user no longer exists.");

        return ToResponse(user);
    }

    public async Task<CurrentUserResponse> UpdateAsync(
        Guid userId,
        UpdateUserProfileRequest request,
        CancellationToken cancellationToken)
    {
        var user = await store.FindUserAsync(userId, cancellationToken)
            ?? throw new IdentityUnauthorizedException("The authenticated user no longer exists.");

        if (!string.IsNullOrWhiteSpace(request.NationalCode) &&
            (request.NationalCode.Length != 10 || !request.NationalCode.All(char.IsDigit)))
        {
            throw new IdentityValidationException("National code must contain exactly 10 digits.");
        }

        if (!string.IsNullOrWhiteSpace(request.Avatar) && request.Avatar.Length > 2_500_000)
        {
            throw new IdentityValidationException("Avatar is too large.");
        }

        user.UpdateProfile(request.FirstName, request.LastName, request.NationalCode, request.Gender, request.Avatar);
        await store.UpdateUserAsync(user, cancellationToken);
        return ToResponse(user);
    }

    private static CurrentUserResponse ToResponse(Bilito.Identity.Domain.Users.User user) =>
        new(user.Id, user.Mobile, user.MobileVerified, user.FirstName, user.LastName,
            user.NationalCode, user.Gender, user.Avatar);
}
