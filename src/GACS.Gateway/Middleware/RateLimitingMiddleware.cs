namespace GACS.Gateway.Middleware;

public sealed class RateLimitingMiddleware(RequestDelegate next)
{
    // TODO: Read TenantId from context. Increment Redis counter (DB 2) using sliding window.
    //       Return 429 with Retry-After header if rate limit exceeded.
    //       Limits are configured per-tenant in tenant config cache.
    public Task InvokeAsync(HttpContext context) => next(context);
}
