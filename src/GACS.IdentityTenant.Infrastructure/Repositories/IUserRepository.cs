using ErrorOr;
using GACS.IdentityTenant.Domain.Entities;
using GACS.Shared.Pagination;
using GACS.Shared.Responses;

namespace GACS.IdentityTenant.Infrastructure.Repositories;

public interface IUserRepository
{
    Task<ErrorOr<User>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ErrorOr<User>> GetByEmailAsync(Guid tenantId, string email, CancellationToken ct = default);
    Task<ErrorOr<PagedResult<User>>> GetByTenantAsync(Guid tenantId, PaginationParameters pagination, CancellationToken ct = default);
    Task<ErrorOr<User>> CreateAsync(User user, CancellationToken ct = default);
    Task<ErrorOr<User>> UpdateAsync(User user, CancellationToken ct = default);
    Task<ErrorOr<bool>> SoftDeleteAsync(Guid id, string deletedBy, CancellationToken ct = default);
}
