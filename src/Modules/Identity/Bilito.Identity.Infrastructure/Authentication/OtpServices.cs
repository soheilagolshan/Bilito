using System.Security.Cryptography;
using System.Text;
using System.Globalization;
using Bilito.Identity.Application.Abstractions;

namespace Bilito.Identity.Infrastructure.Authentication;

public sealed class SecureOtpCodeGenerator : IOtpCodeGenerator
{
    public string Generate(int length)
    {
        if (length != 6)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Only six-digit OTPs are supported.");
        }

        return RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
    }
}

public sealed class Sha256OtpCodeHasher : IOtpCodeHasher
{
    public string Hash(Guid challengeId, string mobile, string code) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            string.Concat(challengeId.ToString("N"), ":", mobile, ":", code))));

    public bool Verify(Guid challengeId, string mobile, string code, string hash) =>
        CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(Hash(challengeId, mobile, code)),
            Convert.FromHexString(hash));
}
