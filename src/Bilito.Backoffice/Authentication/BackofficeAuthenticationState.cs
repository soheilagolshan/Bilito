using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace Bilito.Backoffice.Authentication;

public sealed class BackofficeAuthenticationState : AuthenticationStateProvider
{
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());
    private ClaimsPrincipal _currentUser = Anonymous;

    public string? AccessToken { get; private set; }

    public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
        Task.FromResult(new AuthenticationState(_currentUser));

    public void SetAccessToken(string accessToken)
    {
        AccessToken = accessToken;
        _currentUser = new ClaimsPrincipal(new ClaimsIdentity(ParseClaims(accessToken), "Bearer", ClaimTypes.Name, ClaimTypes.Role));
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public void Clear()
    {
        AccessToken = null;
        _currentUser = Anonymous;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private static List<Claim> ParseClaims(string token)
    {
        var parts = token.Split('.');
        if (parts.Length < 2)
        {
            return [];
        }

        try
        {
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            using var document = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
            var claims = new List<Claim>();
            foreach (var property in document.RootElement.EnumerateObject())
            {
                var value = property.Value.ValueKind == JsonValueKind.Array
                    ? string.Join(",", property.Value.EnumerateArray().Select(item => item.ToString()))
                    : property.Value.ToString();
                var claimType = property.Name switch
                {
                    "sub" => ClaimTypes.NameIdentifier,
                    "name" => ClaimTypes.Name,
                    "role" => ClaimTypes.Role,
                    _ => property.Name
                };
                claims.Add(new Claim(claimType, value));
            }

            return claims;
        }
        catch (FormatException)
        {
            return [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
