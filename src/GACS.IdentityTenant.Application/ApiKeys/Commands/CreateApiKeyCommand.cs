using ErrorOr;
using GACS.IdentityTenant.Domain.Entities;

namespace GACS.IdentityTenant.Application.ApiKeys.Commands;

public sealed record CreateApiKeyCommand(
    Guid TenantId,
    string Name,
    DateTime? ExpiresAt,
    string CreatedBy);

public sealed record CreateApiKeyResult(ApiKey ApiKey, string RawKey);

public sealed class CreateApiKeyHandler
{
    // TODO: Inject IApiKeyRepository
    // IMPORTANT: Hash the raw key before storing. Return raw key ONCE — never stored.
    public Task<ErrorOr<CreateApiKeyResult>> Handle(CreateApiKeyCommand command) =>
        throw new NotImplementedException();
}
