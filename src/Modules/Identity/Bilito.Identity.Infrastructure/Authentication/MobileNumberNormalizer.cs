using Bilito.Identity.Application.Abstractions;

namespace Bilito.Identity.Infrastructure.Authentication;

public sealed class MobileNumberNormalizer : IMobileNumberNormalizer
{
    public string Normalize(string mobile)
    {
        var value = mobile.Trim()
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("(", string.Empty, StringComparison.Ordinal)
            .Replace(")", string.Empty, StringComparison.Ordinal);

        if (value.StartsWith("+98", StringComparison.Ordinal))
        {
            value = value[1..];
        }

        if (value.StartsWith("09", StringComparison.Ordinal) && value.Length == 11)
        {
            value = "98" + value[1..];
        }

        if (value.Length != 12 || !value.StartsWith("989", StringComparison.Ordinal) ||
            value.Any(character => character is < '0' or > '9'))
        {
            throw new IdentityValidationException("A valid Iranian mobile number is required.");
        }

        return "+" + value;
    }
}
