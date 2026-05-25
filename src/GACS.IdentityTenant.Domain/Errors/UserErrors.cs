using ErrorOr;

namespace GACS.IdentityTenant.Domain.Errors;

public static class UserErrors
{
    public static readonly Error NotFound =
        Error.NotFound("User.NotFound", "The requested user was not found.");

    public static readonly Error AlreadyExists =
        Error.Conflict("User.AlreadyExists", "A user with this email already exists in this tenant.");

    public static readonly Error InvalidRole =
        Error.Validation("User.InvalidRole", "The specified role is not valid.");
}
