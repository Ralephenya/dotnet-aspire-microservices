using GACS.IdentityTenant.Infrastructure.Data;

namespace GACS.IdentityTenant.Infrastructure.Jobs;

public sealed class ConsentExpiryReminderJob(IDapperConnectionFactory connectionFactory) : IHangfireJob
{
    // Runs: daily 06:00 SAST (04:00 UTC) — cron: "0 4 * * *"
    // Finds consent records expiring within 30 days
    // In Phase 1: writes to notification_queue table for manual follow-up
    // In Phase 2: will queue to Notification Service via Service Bus
    // Does not send directly — never triggers external services from a background job in Phase 1
    public Task ExecuteAsync(CancellationToken ct = default) => throw new NotImplementedException();
}
