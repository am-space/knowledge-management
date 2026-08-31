using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Knowledge.Server.Infrastructure.Persistence;

public sealed class PersistenceHealthCheck(
    KnowledgeDbContext dbContext,
    IOptions<PersistenceOptions> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await dbContext.Database.OpenConnectionAsync(cancellationToken);
            await dbContext.Database.CloseConnectionAsync();
            return HealthCheckResult.Healthy("Persistence is available.", new Dictionary<string, object>
            {
                ["provider"] = options.Value.ParseProvider().ToString(),
            });
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Persistence is unavailable.", exception);
        }
    }
}
