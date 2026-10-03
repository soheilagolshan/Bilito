using System.Text.Json.Serialization;

namespace Bilito.Identity.Contracts.Authentication;

public sealed record RequestOtpRequest(string Mobile);

public sealed record RequestOtpResponse(
    int ExpiresInSeconds,
    int ResendAfterSeconds,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? DevelopmentOtp);

public sealed record VerifyOtpRequest(string Mobile, string Otp);

public sealed record AuthenticationResponse(string AccessToken, int ExpiresInSeconds);
