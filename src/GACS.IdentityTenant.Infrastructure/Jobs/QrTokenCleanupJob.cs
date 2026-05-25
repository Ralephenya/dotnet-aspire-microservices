using GACS.IdentityTenant.Infrastructure.Cache;

namespace GACS.IdentityTenant.Infrastructure.Jobs;

public sealed class QrTokenCleanupJob(IRedisCacheService cache) : IHangfireJob
{
    // Runs: every 5 minutes — cron: "*/5 * * * *"
    // Defensive cleanup of expired QR tokens in Redis DB 1
    // Redis TTL (120s) handles most expiry — this cleans edge cases
    // Must be idempotent: running twice has no side effects
    public Task ExecuteAsync(CancellationToken ct = default) => throw new NotImplementedException();
}
