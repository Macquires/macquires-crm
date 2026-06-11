using Application.Common.Telecom.SellingLine;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Telecom;

public sealed class ActivationChannelContext : IActivationChannelContext
{
    public const string ClientChannelHeader = "X-Client-Channel";

    private readonly IHttpContextAccessor _http;

    public ActivationChannelContext(IHttpContextAccessor http) => _http = http;

    public bool IsDigitalGatewayRequest => IsDigitalGateway(_http.HttpContext);

    internal static bool IsDigitalGateway(HttpContext? context)
    {
        if (context == null)
        {
            return false;
        }

        if (context.Request.Headers.TryGetValue(ClientChannelHeader, out var values))
        {
            var header = values.ToString();
            if (header.Equals("Digital", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        var path = context.Request.Path.Value ?? string.Empty;
        return path.Contains("/api/public/", StringComparison.OrdinalIgnoreCase)
               || path.Contains("/api/digital/", StringComparison.OrdinalIgnoreCase);
    }
}
