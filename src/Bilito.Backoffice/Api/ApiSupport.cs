using System.Text.Json;

namespace Bilito.Backoffice.Api;

internal static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}

internal static class ProblemDetailsParser
{
    public static ApiProblemDetails Parse(string body, int statusCode)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            Dictionary<string, string[]>? errors = null;
            if (root.TryGetProperty("errors", out var errorsElement))
            {
                errors = errorsElement.EnumerateObject()
                    .ToDictionary(
                        property => property.Name,
                        property => property.Value.EnumerateArray().Select(value => value.GetString() ?? string.Empty).ToArray());
            }

            return new ApiProblemDetails(
                GetString(root, "title"),
                root.TryGetProperty("status", out var status) ? status.GetInt32() : statusCode,
                GetString(root, "detail"),
                errors);
        }
        catch (JsonException)
        {
            return new ApiProblemDetails("Request failed", statusCode, body, null);
        }
    }

    private static string? GetString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var property) ? property.GetString() : null;
}
