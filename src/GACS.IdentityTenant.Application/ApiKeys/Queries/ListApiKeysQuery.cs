using ErrorOr;
using GACS.IdentityTenant.Domain.Entities;
using GACS.Shared.Pagination;
using GACS.Shared.Responses;

namespace GACS.IdentityTenant.Application.ApiKeys.Queries;

public sealed record ListApiKeysQuery(Guid TenantId, PaginationParameters Pagination);

public sealed class ListApiKeysHandler
{
    // TODO: Inject IApiKeyRepository
    public Task<ErrorOr<PagedResult<ApiKey>>> Handle(ListApiKeysQuery query) =>
        throw new NotImplementedException();
}
