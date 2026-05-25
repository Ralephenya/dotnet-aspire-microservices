using ErrorOr;
using GACS.IdentityTenant.Domain.Entities;

namespace GACS.IdentityTenant.Application.Tenants.Commands;

public sealed record CreateTenantCommand(
    string Name,
    string Slug,
    string ContactEmail,
    string? ContactPhone,
    string CreatedBy);

public sealed class CreateTenantHandler
{
    // TODO: Inject ITenantRepository
    // Return ErrorOr<Tenant> — no try/catch, no HTTP concerns
    public Task<ErrorOr<Tenant>> Handle(CreateTenantCommand command) =>
        throw new NotImplementedException();
}
