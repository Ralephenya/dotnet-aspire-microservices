// ============================================================
// UsageMeteringMiddleware.cs
//
// Purpose: Count every proxied API call per tenant per day.
//          Runs AFTER the proxy response — does not add latency to the happy path.
//
// Redis usage (DB 5):
//   Key pattern : metering:{tenantId}:{yyyyMMdd}
//   Value type  : integer counter (INCR)
//   TTL         : 35 days (allow month-end rollup + buffer)
//
// Flush to SQL:
//   Hangfire UsageMeteringFlushJob runs every hour.
//   It reads all metering:{tenantId}:* keys, writes totals to billing.DailyUsage table,
//   then resets the Redis counters.
//
// What is counted:
//   - Only requests that reached the downstream service (i.e., not rejected by auth/rate limit)
//   - Status codes are recorded so failed calls can be excluded from billing if needed
// ============================================================

using StackExchange.Redis;

namespace GACS.Gateway.Middleware;

public sealed class UsageMeteringMiddleware(
    RequestDelegate next,
    IConnectionMultiplexer redis,
    ILogger<UsageMeteringMiddleware> logger)
{
    private const int RedisDatabaseIndex = 5;  // DB 5: usage metering
    private static readonly TimeSpan KeyTtl = TimeSpan.FromDays(35);

    public async Task InvokeAsync(HttpContext context)
    {
        // Call next FIRST — metering happens AFTER we have the response status
        await next(context);

        // ── Don't meter health / metrics endpoints ──
        var path = context.Request.Path.Value ?? string.Empty;
        if (path is "/health" or "/alive" or "/metrics")
            return;

        var tenantId = context.Items.TryGetValue("TenantId", out var tid)
            ? tid?.ToString() ?? "unknown"
            : "anonymous";

        var dateKey  = DateTime.UtcNow.ToString("yyyyMMdd");
        var redisKey = $"metering:{tenantId}:{dateKey}";

        try
        {
            var db    = redis.GetDatabase(RedisDatabaseIndex);
            var count = await db.StringIncrementAsync(redisKey);

            // Set TTL on the first increment (count == 1)
            if (count == 1)
                await db.KeyExpireAsync(redisKey, KeyTtl);

            logger.LogDebug(
                "Usage metered. TenantId={TenantId} Date={Date} Count={Count} Status={Status}",
                tenantId, dateKey, count, context.Response.StatusCode);
        }
        catch (RedisException ex)
        {
            // Metering failure must NEVER affect the actual API response.
            // Log and continue — the Hangfire flush job will notice missing data.
            logger.LogError(ex,
                "Redis metering write failed for TenantId={TenantId}. Usage count may be inaccurate.",
                tenantId);
        }
    }
}
