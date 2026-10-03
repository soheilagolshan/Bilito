namespace Bilito.Backoffice.Api;

public sealed class SystemApiClient(HttpClient httpClient, ApiRequestLog requestLog)
{
    public async Task<ApiResponse<string>> GetHealthAsync(CancellationToken cancellationToken)
    {
        const string endpoint = "health";
        using var response = await httpClient.GetAsync(endpoint, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var problem = response.IsSuccessStatusCode ? null : ProblemDetailsParser.Parse(body, (int)response.StatusCode);
        requestLog.Record(new ApiRequestInfo("GET", endpoint, (int)response.StatusCode, body, problem));
        return response.IsSuccessStatusCode
            ? new ApiResponse<string>(true, body, "GET", endpoint, (int)response.StatusCode, null)
            : new ApiResponse<string>(false, default, "GET", endpoint, (int)response.StatusCode, problem!);
    }
}
