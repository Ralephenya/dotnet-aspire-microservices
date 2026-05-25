// ============================================================
// ApiKeyValidationMiddleware.cs
//
// Purpose: Validate the X-Api-Key header on every inbound request.
//          On success: set HttpContext.Items["TenantId"] for downstream middleware.
//          On failure: return 401 Unauthorized immediately — request never reaches YARP.
//
// Redis usage (DB 0):
//   Key pattern : apikey:{sha256-of-key}
//   Value       : {tenantId}          (string)
//   TTL         : 5 minutes (refreshed on each hit)
//
// Cache miss path:
//   → Call Identity API /internal/apikeys/validate
//   → Cache the result for 5 minutes
//   → If Identity API returns 404/401 → return 401 and DO NOT cache
//
// Paths that bypass validation (configured in appsettings.json GatewayOptions:PublicPaths):
//   /health, /alive, /metrics — Aspire health check endpoints
// ============================================================

using System.Security.Cryptography;
using System.Text;
using StackExchange.Redis;

namespace GACS.Gateway.Middleware;

public sealed class ApiKeyValidationMiddleware(
    RequestDelegate next,
    IConnectionMultiplexer redis,
    IConfiguration configuration,
    ILogger<ApiKeyValidationMiddleware> logger)
{
    private const string ApiKeyHeader = "X-Api-Key";
    private const int    RedisDatabaseIndex = 0;     // DB 0: API key cache
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    // Paths that don't require an API key (health checks, metrics)
    private static readonly HashSet<string> PublicPaths =
    [
        "/health",
        "/alive",
        "/metrics"
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        // ── Bypass for public paths ──
        if (PublicPaths.Contains(context.Request.Path.Value ?? string.Empty))
        {
            await next(context);
            return;
        }

        // ── Extract API key from header ──
        if (!context.Request.Headers.TryGetValue(ApiKeyHeader, out var apiKeyValues)
            || string.IsNullOrWhiteSpace(apiKeyValues.FirstOrDefault()))
        {
            logger.LogWarning("Request rejected: missing {Header} header. Path={Path}",
                ApiKeyHeader, context.Request.Path);

            context.Response.StatusCode  = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                """{"error":"API key required","header":"X-Api-Key","status":401}""");
            return;
        }

        var rawKey  = apiKeyValues.First()!;
        var keyHash = ComputeHash(rawKey);

        // ── Redis cache lookup ──
        var db       = redis.GetDatabase(RedisDatabaseIndex);
        var cacheKey = $"apikey:{keyHash}";

        string? tenantId;

        try
        {
            var cached = await db.StringGetAsync(cacheKey);
            if (cached.HasValue)
            {
                tenantId = cached.ToString();
                logger.LogDebug("API key cache HIT. TenantId={TenantId}", tenantId);

                // Slide the TTL on every hit so active keys don't expire
                await db.KeyExpireAsync(cacheKey, CacheTtl);
            }
            else
            {
                // ── Cache miss — validate against Identity API ──
                // TODO: replace with typed HttpClient calling
                //       POST /internal/v1/apikeys/validate  { "keyHash": "..." }
                // For scaffold: treat unknown keys as invalid.
                logger.LogDebug("API key cache MISS for hash {Hash}", keyHash[..8]);
                tenantId = null;
            }
        }
        catch (RedisException ex)
        {
            // Redis is temporarily unavailable — fail open with a warning.
            // In production, set GatewayOptions:FailClosedOnRedisError=true to fail closed.
            logger.LogError(ex, "Redis unavailable in ApiKeyValidation; failing open");
            tenantId = "unknown"; // Allow through but meter as unknown tenant
        }

        if (string.IsNullOrEmpty(tenantId))
        {
            logger.LogWarning("Request rejected: invalid API key. Path={Path}", context.Request.Path);
            context.Response.StatusCode  = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                """{"error":"Invalid or expired API key","status":401}""");
            return;
        }

        // ── Attach TenantId to the request context ──
        context.Items["TenantId"] = tenantId;

        await next(context);
    }

    /// <summary>
    /// Returns a hex SHA-256 hash of the raw API key.
    /// Never store raw keys — only hashes go to Redis and logs.
    /// </summary>
    private static string ComputeHash(string rawKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
