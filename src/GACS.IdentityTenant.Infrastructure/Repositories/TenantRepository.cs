using ErrorOr;
using GACS.IdentityTenant.Domain.Entities;
using GACS.IdentityTenant.Domain.Errors;
using GACS.IdentityTenant.Infrastructure.Data;
using GACS.Shared.Pagination;
using GACS.Shared.Responses;

namespace GACS.IdentityTenant.Infrastructure.Repositories;

internal sealed class TenantRepository(IDapperConnectionFactory connectionFactory) : ITenantRepository
{
    public Task<ErrorOr<Tenant>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        // TODO: Call stored proc identity.usp_Tenant_GetById via Dapper
        throw new NotImplementedException();
    }

    public Task<ErrorOr<Tenant>> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        // TODO: Call stored proc identity.usp_Tenant_GetBySlug via Dapper
        throw new NotImplementedException();
    }

    public Task<ErrorOr<PagedResult<Tenant>>> GetAllAsync(PaginationParameters pagination, CancellationToken ct = default)
    {
        // TODO: Call stored proc identity.usp_Tenant_GetAll with pagination via Dapper
        throw new NotImplementedException();
    }

    public Task<ErrorOr<Tenant>> CreateAsync(Tenant tenant, CancellationToken ct = default)
    {
        // TODO: Call stored proc identity.usp_Tenant_Create via Dapper
        //       Stored proc writes audit.AuditLog row in same transaction
        throw new NotImplementedException();
    }

    public Task<ErrorOr<Tenant>> UpdateAsync(Tenant tenant, CancellationToken ct = default)
    {
        // TODO: Call stored proc identity.usp_Tenant_Update via Dapper
        throw new NotImplementedException();
    }

    public Task<ErrorOr<bool>> SoftDeleteAsync(Guid id, string deletedBy, CancellationToken ct = default)
    {
        // TODO: Call stored proc identity.usp_Tenant_SoftDelete via Dapper
        //       Sets IsDeleted=1, DeletedAt=GETUTCDATE(), DeletedBy — never physically deletes
        throw new NotImplementedException();
    }
}
