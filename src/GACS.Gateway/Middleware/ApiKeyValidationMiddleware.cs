namespace GACS.Gateway.Middleware;

public sealed class ApiKeyValidationMiddleware(RequestDelegate next)
{
    // TODO: Read API key from X-Api-Key header, validate against Redis cache (DB 0),
    //       resolve TenantId, attach to HttpContext.Items["TenantId"].
    //       Return 401 if key missing or invalid.
    public Task InvokeAsync(HttpContext context) => next(context);
}
