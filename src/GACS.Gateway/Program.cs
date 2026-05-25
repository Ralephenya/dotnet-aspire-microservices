// ============================================================
// GACS.Gateway — YARP reverse proxy entry point
//
// Request pipeline (order matters — do NOT rearrange):
//   1. ApiKeyValidationMiddleware  — validates X-Api-Key header, sets TenantId in Items
//   2. TenantResolutionMiddleware  — resolves full tenant context from TenantId
//   3. UseRateLimiter              — sliding-window limiter, partitioned by TenantId
//   4. UsageMeteringMiddleware     — wraps proxy call, increments Redis counter after response
//   5. MapReverseProxy             — YARP forwards request to downstream service
//
// Rate limiting:
//   In-process sliding window (1,000 req/min per tenant) for single-instance deployments.
//   For horizontal scaling, swap PartitionedRateLimiter.Create for a Redis-backed limiter:
//   https://github.com/cristipufu/aspnetcore-redis-rate-limiting
//
// Redis databases (set in appsettings.json RedisDatabases section):
//   DB 0 — API key cache        (ApiKeyValidationMiddleware)
//   DB 2 — Rate limit counters  (future: distributed limiter)
//   DB 5 — Usage metering       (UsageMeteringMiddleware)
//
// YARP routes: see appsettings.json ReverseProxy section
// ============================================================

using System.Threading.RateLimiting;
using GACS.Gateway.Middleware;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// ── Redis connection — shared across all gateway middleware ──
// Connection string is injected by Aspire via WithReference(redis) in AppHost.
var redisConnectionString = builder.Configuration.GetConnectionString("gacs-redis")
    ?? "localhost:6379";

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var config = ConfigurationOptions.Parse(redisConnectionString);
    config.AbortOnConnectFail = false;   // Retry on startup; don't crash if Redis briefly unavailable
    config.ConnectRetry = 5;
    config.ReconnectRetryPolicy = new ExponentialRetry(5000, 30000);
    return ConnectionMultiplexer.Connect(config);
});

// ── YARP reverse proxy — routes loaded from appsettings.json ──
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// ── Rate limiter — sliding window, partitioned per tenant ──
//
// Why sliding window (not fixed)?
//   Fixed window allows a burst of 2× the limit at window boundaries.
//   Sliding window smooths traffic so 1,000 req/min really means ≤1,000 at any rolling minute.
//
// Why in-process (not Redis-backed)?
//   For a single Aspire instance this is sufficient and zero-dependency.
//   To scale the gateway horizontally, replace with a Redis-backed limiter and add
//   the NuGet package: RedisRateLimiting by Cristian Pufu.
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
    {
        // Partition key = TenantId (set by ApiKeyValidationMiddleware).
        // Falls back to IP address for unauthenticated / health-check requests.
        var key = ctx.Items.TryGetValue("TenantId", out var tid)
            && tid is string tenantId
            && !string.IsNullOrEmpty(tenantId)
                ? $"tenant:{tenantId}"
                : $"ip:{ctx.Connection.RemoteIpAddress}";

        return RateLimitPartition.GetSlidingWindowLimiter(key, _ =>
            new SlidingWindowRateLimiterOptions
            {
                PermitLimit          = 1_000,                 // requests allowed per window
                Window               = TimeSpan.FromMinutes(1),
                SegmentsPerWindow    = 6,                     // 10-second resolution
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit           = 0                      // reject immediately, no queuing
            });
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (ctx, token) =>
    {
        ctx.HttpContext.Response.Headers["Retry-After"] = "60";
        ctx.HttpContext.Response.ContentType = "application/json";
        await ctx.HttpContext.Response.WriteAsync(
            """{"error":"Rate limit exceeded","retryAfterSeconds":60,"status":429}""",
            token);
    };
});

var app = builder.Build();

// Health + metrics endpoints (from ServiceDefaults — /health, /alive, /metrics)
app.MapDefaultEndpoints();

// ── Middleware pipeline — order is load-bearing ──
app.UseMiddleware<ApiKeyValidationMiddleware>();  // 1. Auth: set TenantId or reject 401
app.UseMiddleware<TenantResolutionMiddleware>();  // 2. Context: full tenant object
app.UseRateLimiter();                            // 3. Throttle: 429 if over limit
app.UseMiddleware<UsageMeteringMiddleware>();     // 4. Meter: wraps proxy, counts after

// YARP: forward request to downstream service
app.MapReverseProxy();

// Root redirect to health (useful when checking gateway directly in browser)
app.MapGet("/", () => Results.Redirect("/health"));

app.Run();
