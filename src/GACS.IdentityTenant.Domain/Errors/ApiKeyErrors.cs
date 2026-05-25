using ErrorOr;

namespace GACS.IdentityTenant.Domain.Errors;

public static class ApiKeyErrors
{
    public static readonly Error NotFound =
        Error.NotFound("ApiKey.NotFound", "The requested API key was not found.");

    public static readonly Error Invalid =
        Error.Unauthorized("ApiKey.Invalid", "The API key is invalid or has expired.");

    public static readonly Error LimitReached =
        Error.Conflict("ApiKey.LimitReached", "This tenant has reached the maximum number of active API keys.");
}
