using GACS.IdentityTenant.Infrastructure.Data;

namespace GACS.IdentityTenant.Infrastructure.Jobs;

public sealed class StaleVisitCleanupJob(IDapperConnectionFactory connectionFactory) : IHangfireJob
{
    // Runs: every hour — cron: "0 * * * *"
    // Finds active visits where CheckedInAt < GETUTCDATE() - 12 hours and Status = 'Active'
    // Sets Status = 'Expired'
    // Writes to audit.AuditLog
    // Must be idempotent: already-expired visits must not be re-processed
    public Task ExecuteAsync(CancellationToken ct = default) => throw new NotImplementedException();
}
