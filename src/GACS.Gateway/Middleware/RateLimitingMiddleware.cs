// ============================================================
// RateLimitingMiddleware.cs — REPLACED
//
// Rate limiting is now handled by ASP.NET Core's built-in middleware:
//   app.UseRateLimiter()  (configured in Program.cs)
//
// This file is kept as a reference stub. DO NOT register this class
// in the middleware pipeline — UseRateLimiter() is already there.
//
// Why the change?
//   ASP.NET Core's PartitionedRateLimiter integrates with YARP's route config
//   (RequireRateLimiting policy name on a route), returns correct 429 + Retry-After
//   headers automatically, and exposes metrics to OpenTelemetry with no extra code.
//
// For distributed rate limiting (multi-instance gateway):
//   Replace PartitionedRateLimiter.Create with the Redis-backed equivalent:
//   NuGet: RedisRateLimiting by Cristian Pufu
//   Docs:  https://github.com/cristipufu/aspnetcore-redis-rate-limiting
// ============================================================

namespace GACS.Gateway.Middleware;

[Obsolete("Rate limiting is handled by app.UseRateLimiter() in Program.cs. Do not add this to the pipeline.")]
public sealed class RateLimitingMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context) => next(context);
}
