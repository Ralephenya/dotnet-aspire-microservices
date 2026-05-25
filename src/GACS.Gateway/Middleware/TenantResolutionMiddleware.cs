// ============================================================
// TenantResolutionMiddleware.cs
//
// Purpose: After ApiKeyValidationMiddleware sets HttpContext.Items["TenantId"],
//          this middleware enriches the context with the full tenant object.
//
// Pipeline position: AFTER ApiKeyValidationMiddleware, BEFORE UseRateLimiter()
//   → ApiKeyValidationMiddleware sets Items["TenantId"]
//   → TenantResolutionMiddleware reads it, loads TenantContext, sets Items["Tenant"]
//   → UseRateLimiter() partitions by tenant-level rate limit config
//   → UsageMeteringMiddleware records the call under the resolved tenant
//
// Redis usage (DB 3):
//   Key pattern : tenant:config:{tenantId}
//   Value type  : JSON (TenantContext serialized)
//   TTL         : 15 minutes (refreshed on hit)
//
// Cache miss path:
//   → Call Identity API GET /internal/v1/tenants/{tenantId}
//   → If tenant is Suspended: return 403 Forbidden
//   → If tenant is not found: return 403 Forbidden (never 404 — avoids tenant enumeration)
//   → Cache for 15 minutes
//
// What is set on success:
//   HttpContext.Items["Tenant"] = TenantContext record (see below)
//
// Implementation guide for junior devs:
//   1. Add a TenantContext record to GACS.Shared/Responses/TenantContext.cs
//      with: TenantId, Name, Tier (Free/Pro/Enterprise), IsActive, RateLimitOverride?
//   2. Register a typed IHttpClientFactory HttpClient for "identity-internal"
//      pointing to https+http://gacs-identitytenant-api in Program.cs
//   3. Replace the TODO below with an HttpClient call + Redis cache read/write
//   4. Add System.Text.Json serialization to/from the Redis string value
// ============================================================

using StackExchange.Redis;

namespace GACS.Gateway.Middleware;

public sealed class TenantResolutionMiddleware(
    RequestDelegate next,
    IConnectionMultiplexer redis,
    ILogger<TenantResolutionMiddleware> logger)
{
    private const int    RedisDatabaseIndex = 3;   // DB 3: tenant config cache
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(15);

    // Paths that already bypassed ApiKeyValidation — no tenant to resolve
    private static readonly HashSet<string> SkipPaths =
    [
        "/health",
        "/alive",
        "/metrics"
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        // ── Skip health / metrics paths ──
        if (SkipPaths.Contains(context.Request.Path.Value ?? string.Empty))
        {
            await next(context);
            return;
        }

        // ── TenantId must already be set by ApiKeyValidationMiddleware ──
        if (!context.Items.TryGetValue("TenantId", out var tid)
            || tid is not string tenantId
            || string.IsNullOrEmpty(tenantId)
            || tenantId == "unknown")
        {
            // No valid TenantId — pass through (ApiKeyValidation already decided auth)
            await next(context);
            return;
        }

        // ── TODO: Load full tenant context ──
        //
        // Replace the scaffold block below with real implementation:
        //
        //   var db         = redis.GetDatabase(RedisDatabaseIndex);
        //   var cacheKey   = $"tenant:config:{tenantId}";
        //   var cached     = await db.StringGetAsync(cacheKey);
        //
        //   TenantContext? tenant;
        //   if (cached.HasValue)
        //   {
        //       tenant = JsonSerializer.Deserialize<TenantContext>(cached!);
        //       await db.KeyExpireAsync(cacheKey, CacheTtl);   // slide TTL
        //   }
        //   else
        //   {
        //       // Call Identity API internal endpoint
        //       var httpClient = httpClientFactory.CreateClient("identity-internal");
        //       var response   = await httpClient.GetAsync($"/internal/v1/tenants/{tenantId}");
        //
        //       if (!response.IsSuccessStatusCode)
        //       {
        //           context.Response.StatusCode = StatusCodes.Status403Forbidden;
        //           await context.Response.WriteAsync("""{"error":"Tenant not found or suspended","status":403}""");
        //           return;
        //       }
        //
        //       tenant = await response.Content.ReadFromJsonAsync<TenantContext>();
        //       if (tenant is null || !tenant.IsActive)
        //       {
        //           context.Response.StatusCode = StatusCodes.Status403Forbidden;
        //           await context.Response.WriteAsync("""{"error":"Tenant is suspended","status":403}""");
        //           return;
        //       }
        //
        //       var json = JsonSerializer.Serialize(tenant);
        //       await db.StringSetAsync(cacheKey, json, CacheTtl);
        //   }
        //
        //   context.Items["Tenant"] = tenant;

        // SCAFFOLD: pass through without enrichment until Identity API is implemented
        logger.LogDebug("TenantResolutionMiddleware: scaffold pass-through for TenantId={TenantId}", tenantId);

        await next(context);
    }
}
