using GACS.IdentityTenant.Infrastructure.Cache;
using GACS.IdentityTenant.Infrastructure.Data;

namespace GACS.IdentityTenant.Infrastructure.Jobs;

public sealed class UsageMeteringFlushJob(
    IRedisCacheService cache,
    IDapperConnectionFactory connectionFactory) : IHangfireJob
{
    // Runs: every 5 minutes — cron: "*/5 * * * *"
    // Reads per-tenant API call counters from Redis DB 5
    // Writes totals to billing table in SQL Server
    // Resets Redis counters (atomic: read + reset in one operation)
    // Must be idempotent: if flush fails, next run will catch up
    public Task ExecuteAsync(CancellationToken ct = default) => throw new NotImplementedException();
}
