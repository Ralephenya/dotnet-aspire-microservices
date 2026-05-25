using ErrorOr;
using GACS.IdentityTenant.Domain.Entities;
using GACS.Shared.Pagination;
using GACS.Shared.Responses;

namespace GACS.IdentityTenant.Application.Tenants.Queries;

public sealed record ListTenantsQuery(PaginationParameters Pagination);

public sealed class ListTenantsHandler
{
    // TODO: Inject ITenantRepository
    public Task<ErrorOr<PagedResult<Tenant>>> Handle(ListTenantsQuery query) =>
        throw new NotImplementedException();
}
