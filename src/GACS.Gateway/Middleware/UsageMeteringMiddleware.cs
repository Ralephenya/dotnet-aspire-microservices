namespace GACS.Gateway.Middleware;

public sealed class UsageMeteringMiddleware(RequestDelegate next)
{
    // TODO: After downstream response, increment per-tenant call counter in Redis DB 5.
    //       Key pattern: metering:{tenantId}:{yyyyMMdd}
    //       Hangfire UsageMeteringFlushJob reads and writes to SQL every 5 minutes.
    public Task InvokeAsync(HttpContext context) => next(context);
}
