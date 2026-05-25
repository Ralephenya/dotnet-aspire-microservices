using ErrorOr;
using GACS.IdentityTenant.Domain.Entities;

namespace GACS.IdentityTenant.Application.Users.Commands;

public sealed record CreateUserCommand(
    Guid TenantId,
    string ExternalId,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    string CreatedBy);

public sealed class CreateUserHandler
{
    // TODO: Inject IUserRepository
    public Task<ErrorOr<User>> Handle(CreateUserCommand command) =>
        throw new NotImplementedException();
}
