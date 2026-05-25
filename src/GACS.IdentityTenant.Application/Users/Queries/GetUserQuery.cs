using ErrorOr;
using GACS.IdentityTenant.Domain.Entities;

namespace GACS.IdentityTenant.Application.Users.Queries;

public sealed record GetUserQuery(Guid UserId);

public sealed class GetUserHandler
{
    // TODO: Inject IUserRepository
    public Task<ErrorOr<User>> Handle(GetUserQuery query) =>
        throw new NotImplementedException();
}
