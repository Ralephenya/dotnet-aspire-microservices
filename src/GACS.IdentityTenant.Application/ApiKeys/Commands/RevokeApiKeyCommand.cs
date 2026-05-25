using ErrorOr;

namespace GACS.IdentityTenant.Application.ApiKeys.Commands;

public sealed record RevokeApiKeyCommand(Guid ApiKeyId, string RevokedBy);

public sealed class RevokeApiKeyHandler
{
    // TODO: Inject IApiKeyRepository, IRedisCacheService
    // Revoke in SQL + invalidate Redis DB 0 cache entry
    public Task<ErrorOr<bool>> Handle(RevokeApiKeyCommand command) =>
        throw new NotImplementedException();
}
