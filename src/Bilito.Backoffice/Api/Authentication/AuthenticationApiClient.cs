using System.Net;
using System.Net.Http.Json;
using Bilito.Identity.Contracts.Authentication;

namespace Bilito.Backoffice.Api.Authentication;

public sealed class AuthenticationApiClient(HttpClient httpClient, ApiRequestLog requestLog)
{
    public Task<ApiResponse<RequestOtpResponse>> RequestOtpAsync(string mobile, CancellationToken cancellationToken) =>
        SendAsync<RequestOtpRequest, RequestOtpResponse>(
            HttpMethod.Post,
            "api/v1/auth/otp/request",
            new RequestOtpRequest(mobile),
            cancellationToken);

    public Task<ApiResponse<AuthenticationResponse>> VerifyOtpAsync(
        string mobile,
        string otp,
        CancellationToken cancellationToken) =>
        SendAsync<VerifyOtpRequest, AuthenticationResponse>(
            HttpMethod.Post,
            "api/v1/auth/otp/verify",
            new VerifyOtpRequest(mobile, otp),
            cancellationToken);

    public Task<ApiResponse<AuthenticationResponse>> RefreshAsync(CancellationToken cancellationToken) =>
        SendAsync<AuthenticationResponse>(HttpMethod.Post, "api/v1/auth/refresh", cancellationToken);

    public Task<ApiResponse<object?>> LogoutAsync(CancellationToken cancellationToken) =>
        SendAsync<object?>(HttpMethod.Post, "api/v1/auth/logout", cancellationToken);

    private async Task<ApiResponse<TResponse>> SendAsync<TRequest, TResponse>(
        HttpMethod method,
        string endpoint,
        TRequest body,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, endpoint)
        {
            Content = JsonContent.Create(body)
        };
        return await SendAsync<TResponse>(request, method.Method, endpoint, cancellationToken);
    }

    private async Task<ApiResponse<TResponse>> SendAsync<TResponse>(
        HttpMethod method,
        string endpoint,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, endpoint);
        return await SendAsync<TResponse>(request, method.Method, endpoint, cancellationToken);
    }

    private async Task<ApiResponse<TResponse>> SendAsync<TResponse>(
        HttpRequestMessage request,
        string method,
        string endpoint,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            TResponse? data = default;
            if (!string.IsNullOrWhiteSpace(body) && response.StatusCode != HttpStatusCode.NoContent)
            {
                data = System.Text.Json.JsonSerializer.Deserialize<TResponse>(body, JsonDefaults.Options);
            }

            requestLog.Record(new ApiRequestInfo(method, endpoint, (int)response.StatusCode, body, null));
            return new ApiResponse<TResponse>(true, data, method, endpoint, (int)response.StatusCode, null);
        }

        var problem = ProblemDetailsParser.Parse(body, (int)response.StatusCode);
        requestLog.Record(new ApiRequestInfo(method, endpoint, (int)response.StatusCode, body, problem));
        return new ApiResponse<TResponse>(false, default, method, endpoint, (int)response.StatusCode, problem);
    }
}
