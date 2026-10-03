using Bilito.Identity.Contracts.Users;
using System.Net.Http.Json;

namespace Bilito.Backoffice.Api.Users;

public sealed class UsersApiClient(HttpClient httpClient, ApiRequestLog requestLog)
{
    public async Task<ApiResponse<CurrentUserResponse>> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        const string endpoint = "api/v1/users/me";
        using var response = await httpClient.GetAsync(endpoint, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var data = System.Text.Json.JsonSerializer.Deserialize<CurrentUserResponse>(body, JsonDefaults.Options);
            requestLog.Record(new ApiRequestInfo("GET", endpoint, (int)response.StatusCode, body, null));
            return new ApiResponse<CurrentUserResponse>(true, data, "GET", endpoint, (int)response.StatusCode, null);
        }

        var problem = ProblemDetailsParser.Parse(body, (int)response.StatusCode);
        requestLog.Record(new ApiRequestInfo("GET", endpoint, (int)response.StatusCode, body, problem));
        return new ApiResponse<CurrentUserResponse>(false, default, "GET", endpoint, (int)response.StatusCode, problem);
    }

    public async Task<ApiResponse<CurrentUserResponse>> UpdateCurrentUserAsync(
        UpdateUserProfileRequest request,
        CancellationToken cancellationToken)
    {
        const string endpoint = "api/v1/users/me";
        using var httpRequest = new HttpRequestMessage(HttpMethod.Put, endpoint)
        {
            Content = JsonContent.Create(request, options: JsonDefaults.Options)
        };
        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var data = System.Text.Json.JsonSerializer.Deserialize<CurrentUserResponse>(body, JsonDefaults.Options);
            requestLog.Record(new ApiRequestInfo("PUT", endpoint, (int)response.StatusCode, body, null));
            return new ApiResponse<CurrentUserResponse>(true, data, "PUT", endpoint, (int)response.StatusCode, null);
        }

        var problem = ProblemDetailsParser.Parse(body, (int)response.StatusCode);
        requestLog.Record(new ApiRequestInfo("PUT", endpoint, (int)response.StatusCode, body, problem));
        return new ApiResponse<CurrentUserResponse>(false, default, "PUT", endpoint, (int)response.StatusCode, problem);
    }
}
