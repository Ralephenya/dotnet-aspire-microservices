using ErrorOr;
using GACS.IdentityTenant.Domain.Entities;
using GACS.IdentityTenant.Infrastructure.Data;
using GACS.Shared.Pagination;
using GACS.Shared.Responses;

namespace GACS.IdentityTenant.Infrastructure.Repositories;

internal sealed class ApiKeyRepository(IDapperConnectionFactory connectionFactory) : IApiKeyRepository
{
    public Task<ErrorOr<ApiKey>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        // TODO: Call stored proc identity.usp_ApiKey_GetById via Dapper
        throw new NotImplementedException();
    }

    public Task<ErrorOr<ApiKey>> GetByHashAsync(string keyHash, CancellationToken ct = default)
    {
        // TODO: Call stored proc identity.usp_ApiKey_GetByHash via Dapper
        //       Used by Gateway middleware — cache result in Redis DB 0 for 1hr
        throw new NotImplementedException();
    }

    public Task<ErrorOr<PagedResult<ApiKey>>> GetByTenantAsync(Guid tenantId, PaginationParameters pagination, CancellationToken ct = default)
    {
        // TODO: Call stored proc identity.usp_ApiKey_GetByTenant via Dapper
        throw new NotImplementedException();
    }

    public Task<ErrorOr<ApiKey>> CreateAsync(ApiKey apiKey, CancellationToken ct = default)
    {
        // TODO: Call stored proc identity.usp_ApiKey_Create via Dapper
        //       Store only the hash — never store raw key value
        throw new NotImplementedException();
    }

    public Task<ErrorOr<bool>> RevokeAsync(Guid id, string revokedBy, CancellationToken ct = default)
    {
        // TODO: Call stored proc identity.usp_ApiKey_Revoke via Dapper
        //       Soft delete + invalidate Redis DB 0 cache entry
        throw new NotImplementedException();
    }
}
