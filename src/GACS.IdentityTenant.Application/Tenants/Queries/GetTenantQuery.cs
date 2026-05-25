using ErrorOr;
using GACS.IdentityTenant.Domain.Entities;

namespace GACS.IdentityTenant.Application.Tenants.Queries;

public sealed record GetTenantQuery(Guid TenantId);

public sealed class GetTenantHandler
{
    // TODO: Inject ITenantRepository
    public Task<ErrorOr<Tenant>> Handle(GetTenantQuery query) =>
        throw new NotImplementedException();
}
