using ErrorOr;
using GACS.IdentityTenant.Domain.Entities;
using GACS.Shared.Pagination;
using GACS.Shared.Responses;

namespace GACS.IdentityTenant.Infrastructure.Repositories;

public interface IApiKeyRepository
{
    Task<ErrorOr<ApiKey>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ErrorOr<ApiKey>> GetByHashAsync(string keyHash, CancellationToken ct = default);
    Task<ErrorOr<PagedResult<ApiKey>>> GetByTenantAsync(Guid tenantId, PaginationParameters pagination, CancellationToken ct = default);
    Task<ErrorOr<ApiKey>> CreateAsync(ApiKey apiKey, CancellationToken ct = default);
    Task<ErrorOr<bool>> RevokeAsync(Guid id, string revokedBy, CancellationToken ct = default);
}
