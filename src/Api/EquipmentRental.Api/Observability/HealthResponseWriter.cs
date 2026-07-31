using System.Text.Json;

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EquipmentRental.Api.Observability;

public static class HealthResponseWriter
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    public static Task WriteAsync(
        HttpContext context,
        HealthReport report)
    {
        context.Response.ContentType = "application/json";

        return context.Response.WriteAsync(
            JsonSerializer.Serialize(
                new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(entry => new
                    {
                        name = entry.Key,
                        status = entry.Value.Status.ToString(),
                        description = entry.Value.Description,
                        data = entry.Value.Data,
                    }),
                    totalDurationMilliseconds =
                        report.TotalDuration.TotalMilliseconds,
                    traceId = context.TraceIdentifier,
                },
                SerializerOptions));
    }
}
