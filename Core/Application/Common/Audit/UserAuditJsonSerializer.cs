using System.Text.Encodings.Web;
using System.Text.Json;

namespace Application.Common.Audit;

public static class UserAuditJsonSerializer
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static string Serialize(object? value) =>
        value == null ? "" : JsonSerializer.Serialize(value, Options);
}
