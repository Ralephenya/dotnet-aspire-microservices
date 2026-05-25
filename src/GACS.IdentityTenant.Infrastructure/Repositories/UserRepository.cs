using ErrorOr;
using GACS.IdentityTenant.Domain.Entities;
using GACS.IdentityTenant.Infrastructure.Data;
using GACS.Shared.Pagination;
using GACS.Shared.Responses;

namespace GACS.IdentityTenant.Infrastructure.Repositories;

internal sealed class UserRepository(IDapperConnectionFactory connectionFactory) : IUserRepository
{
    public Task<ErrorOr<User>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        // TODO: Call stored proc identity.usp_User_GetById via Dapper
        throw new NotImplementedException();
    }

    public Task<ErrorOr<User>> GetByEmailAsync(Guid tenantId, string email, CancellationToken ct = default)
    {
        // TODO: Call stored proc identity.usp_User_GetByEmail via Dapper
        throw new NotImplementedException();
    }

    public Task<ErrorOr<PagedResult<User>>> GetByTenantAsync(Guid tenantId, PaginationParameters pagination, CancellationToken ct = default)
    {
        // TODO: Call stored proc identity.usp_User_GetByTenant via Dapper
        throw new NotImplementedException();
    }

    public Task<ErrorOr<User>> CreateAsync(User user, CancellationToken ct = default)
    {
        // TODO: Call stored proc identity.usp_User_Create via Dapper
        throw new NotImplementedException();
    }

    public Task<ErrorOr<User>> UpdateAsync(User user, CancellationToken ct = default)
    {
        // TODO: Call stored proc identity.usp_User_Update via Dapper
        throw new NotImplementedException();
    }

    public Task<ErrorOr<bool>> SoftDeleteAsync(Guid id, string deletedBy, CancellationToken ct = default)
    {
        // TODO: Call stored proc identity.usp_User_SoftDelete via Dapper
        throw new NotImplementedException();
    }
}
