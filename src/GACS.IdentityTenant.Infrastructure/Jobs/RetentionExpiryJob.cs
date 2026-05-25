using GACS.IdentityTenant.Infrastructure.Data;

namespace GACS.IdentityTenant.Infrastructure.Jobs;

public sealed class RetentionExpiryJob(IDapperConnectionFactory connectionFactory) : IHangfireJob
{
    // Runs: daily 02:00 SAST (00:00 UTC) — cron: "0 0 * * *"
    // Finds visits where RetentionExpiresAt < GETUTCDATE()
    // Soft-deletes personal data fields (Name, Photo, ID number) — not the visit record itself
    // Writes deletion record to audit.AuditLog in the same transaction
    // Rule: never delete without re-verifying retention period in the same transaction
    public Task ExecuteAsync(CancellationToken ct = default) => throw new NotImplementedException();
}
