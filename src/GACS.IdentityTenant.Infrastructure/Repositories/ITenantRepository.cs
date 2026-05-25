using ErrorOr;
using GACS.IdentityTenant.Domain.Entities;
using GACS.Shared.Pagination;
using GACS.Shared.Responses;

namespace GACS.IdentityTenant.Infrastructure.Repositories;

public interface ITenantRepository
{
    Task<ErrorOr<Tenant>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ErrorOr<Tenant>> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<ErrorOr<PagedResult<Tenant>>> GetAllAsync(PaginationParameters pagination, CancellationToken ct = default);
    Task<ErrorOr<Tenant>> CreateAsync(Tenant tenant, CancellationToken ct = default);
    Task<ErrorOr<Tenant>> UpdateAsync(Tenant tenant, CancellationToken ct = default);
    Task<ErrorOr<bool>> SoftDeleteAsync(Guid id, string deletedBy, CancellationToken ct = default);
}
