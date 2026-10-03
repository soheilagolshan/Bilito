using Bilito.Identity.Infrastructure.Authentication;

namespace Bilito.UnitTests;

public sealed class IdentityAuthenticationTests
{
    private readonly MobileNumberNormalizer _normalizer = new();

    [Theory]
    [InlineData("09121234567", "+989121234567")]
    [InlineData("989121234567", "+989121234567")]
    [InlineData("+989121234567", "+989121234567")]
    public void IranianMobileNumbersAreNormalized(string input, string expected)
    {
        Assert.Equal(expected, _normalizer.Normalize(input));
    }

    [Theory]
    [InlineData("0912123456")]
    [InlineData("981212345678")]
    [InlineData("+98912123456x")]
    public void InvalidMobileNumbersAreRejected(string input)
    {
        Assert.Throws<Bilito.Identity.Application.Abstractions.IdentityValidationException>(
            () => _normalizer.Normalize(input));
    }

    [Fact]
    public void GeneratedOtpIsSixNumericDigitsAndHashIsNotTheRawCode()
    {
        var generator = new SecureOtpCodeGenerator();
        var hasher = new Sha256OtpCodeHasher();
        var challengeId = Guid.NewGuid();
        var mobile = "+989121234567";
        var code = generator.Generate(6);
        var hash = hasher.Hash(challengeId, mobile, code);

        Assert.Matches("^[0-9]{6}$", code);
        Assert.NotEqual(code, hash);
        Assert.True(hasher.Verify(challengeId, mobile, code, hash));
        Assert.False(hasher.Verify(challengeId, mobile, "000000", hash));
    }
}
