using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Knowledge.Server;

public static class HealthResponseWriter
{
    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                data = entry.Value.Data,
            }),
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
