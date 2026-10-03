using Bilito.Identity.Application.Abstractions;
using Bilito.Identity.Application.Authentication;
using Bilito.Identity.Application.Configuration;
using Bilito.Identity.Contracts.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Bilito.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthenticationController(
    IdentityAuthenticationService authenticationService,
    IOptions<RefreshTokenOptions> refreshTokenOptions) : ControllerBase
{
    [HttpPost("otp/request")]
    public async Task<ActionResult<RequestOtpResponse>> RequestOtp(RequestOtpRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await authenticationService.RequestOtpAsync(request, cancellationToken));
        }
        catch (IdentityValidationException exception)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Validation failed", detail: exception.Message);
        }
        catch (IdentityConflictException exception)
        {
            return Problem(statusCode: StatusCodes.Status429TooManyRequests, title: "OTP request throttled", detail: exception.Message);
        }
        catch (IdentityDeliveryUnavailableException exception)
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "OTP delivery unavailable", detail: exception.Message);
        }
    }

    [HttpPost("otp/verify")]
    public async Task<ActionResult<AuthenticationResponse>> VerifyOtp(VerifyOtpRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await authenticationService.VerifyOtpAsync(request, cancellationToken);
            SetRefreshTokenCookie(result.RefreshToken);
            return Ok(new AuthenticationResponse(result.AccessToken.Token, GetLifetimeSeconds(result.AccessToken)));
        }
        catch (IdentityValidationException exception)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Validation failed", detail: exception.Message);
        }
        catch (IdentityUnauthorizedException exception)
        {
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Authentication failed", detail: exception.Message);
        }
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthenticationResponse>> Refresh(CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue(refreshTokenOptions.Value.CookieName, out var rawRefreshToken) || string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Authentication failed", detail: "A refresh token is required.");
        }

        try
        {
            var result = await authenticationService.RefreshAsync(rawRefreshToken, cancellationToken);
            SetRefreshTokenCookie(result.RefreshToken);
            return Ok(new AuthenticationResponse(result.AccessToken.Token, GetLifetimeSeconds(result.AccessToken)));
        }
        catch (IdentityUnauthorizedException exception)
        {
            ClearRefreshTokenCookie();
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Authentication failed", detail: exception.Message);
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        Request.Cookies.TryGetValue(refreshTokenOptions.Value.CookieName, out var token);
        await authenticationService.LogoutAsync(token, cancellationToken);
        ClearRefreshTokenCookie();
        return NoContent();
    }

    private void SetRefreshTokenCookie(string token)
    {
        var options = refreshTokenOptions.Value;
        Response.Cookies.Append(options.CookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = options.SecureCookie,
            SameSite = SameSiteMode.Lax,
            Path = "/api/v1/auth",
            MaxAge = TimeSpan.FromDays(options.LifetimeDays)
        });
    }

    private void ClearRefreshTokenCookie() =>
        Response.Cookies.Delete(refreshTokenOptions.Value.CookieName, new CookieOptions { Path = "/api/v1/auth" });

    private static int GetLifetimeSeconds(AccessTokenResult token) =>
        Math.Max(0, (int)(token.ExpiresAt - DateTimeOffset.UtcNow).TotalSeconds);
}
