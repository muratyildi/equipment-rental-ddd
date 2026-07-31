using System.Diagnostics;
using System.Text.RegularExpressions;

namespace EquipmentRental.Api.Observability;

public sealed partial class CorrelationIdMiddleware(
    RequestDelegate next,
    ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var requested = context.Request.Headers[HeaderName].ToString();
        var correlationId =
            requested.Length is > 0 and <= 128
            && SafeCorrelationId().IsMatch(requested)
                ? requested
                : Activity.Current?.TraceId.ToString()
                    ?? Guid.NewGuid().ToString("N");

        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (logger.BeginScope(
                   new Dictionary<string, object>
                   {
                       ["CorrelationId"] = correlationId,
                   }))
        {
            await next(context);
        }
    }

    [GeneratedRegex("^[A-Za-z0-9._:-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeCorrelationId();
}
