namespace GACS.Gateway.Middleware;

public sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    // TODO: Read TenantId from HttpContext.Items set by ApiKeyValidationMiddleware.
    //       Validate tenant is active against SQL (via tenant config Redis cache DB 3).
    //       Return 403 if tenant is suspended or not found.
    public Task InvokeAsync(HttpContext context) => next(context);
}
