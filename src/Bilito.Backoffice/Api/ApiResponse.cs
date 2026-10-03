namespace Bilito.Backoffice.Api;

public sealed record ApiProblemDetails(
    string? Title,
    int? Status,
    string? Detail,
    IReadOnlyDictionary<string, string[]>? Errors);

public sealed record ApiResponse<T>(
    bool Succeeded,
    T? Data,
    string Method,
    string Endpoint,
    int StatusCode,
    ApiProblemDetails? Problem);

public sealed record ApiRequestInfo(
    string Method,
    string Endpoint,
    int StatusCode,
    string? ResponseBody,
    ApiProblemDetails? Problem);

public sealed class ApiRequestLog
{
    public ApiRequestInfo? LastRequest { get; private set; }

    public void Record(ApiRequestInfo request) => LastRequest = request with
    {
        ResponseBody = RedactAccessToken(request.ResponseBody)
    };

    private static string? RedactAccessToken(string? responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return responseBody;
        }

        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(responseBody);
            if (!document.RootElement.TryGetProperty("accessToken", out _))
            {
                return responseBody;
            }

            using var stream = new System.IO.MemoryStream();
            using (var writer = new System.Text.Json.Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                foreach (var property in document.RootElement.EnumerateObject())
                {
                    if (string.Equals(property.Name, "accessToken", StringComparison.OrdinalIgnoreCase))
                    {
                        writer.WriteString(property.Name, "[redacted]");
                    }
                    else
                    {
                        property.WriteTo(writer);
                    }
                }

                writer.WriteEndObject();
            }

            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }
        catch (System.Text.Json.JsonException)
        {
            return responseBody;
        }
    }
}
