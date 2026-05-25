using ErrorOr;

namespace GACS.IdentityTenant.Domain.Errors;

public static class TenantErrors
{
    public static readonly Error NotFound =
        Error.NotFound("Tenant.NotFound", "The requested tenant was not found.");

    public static readonly Error AlreadyExists =
        Error.Conflict("Tenant.AlreadyExists", "A tenant with this slug already exists.");

    public static readonly Error Suspended =
        Error.Forbidden("Tenant.Suspended", "This tenant account is suspended.");

    public static readonly Error InvalidSlug =
        Error.Validation("Tenant.InvalidSlug", "Tenant slug must be lowercase alphanumeric with hyphens only.");
}
