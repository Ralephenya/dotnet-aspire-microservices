using GACS.IdentityTenant.Infrastructure.Data;

namespace GACS.IdentityTenant.Infrastructure.Jobs;

public sealed class AuditLogArchiveJob(IDapperConnectionFactory connectionFactory) : IHangfireJob
{
    // Runs: weekly Sunday 03:00 SAST (01:00 UTC) — cron: "0 1 * * 0"
    // Exports audit.AuditLog entries older than 90 days to Azure Blob Storage (gacs-audit-exports)
    // Marks exported entries as IsArchived = 1 in SQL (not deleted — still queryable)
    // Processes in batches — never run longer than 10 minutes
    // Uses elevated connection (RLS bypassed) — this job runs across all tenants
    public Task ExecuteAsync(CancellationToken ct = default) => throw new NotImplementedException();
}
