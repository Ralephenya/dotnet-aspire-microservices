using Asp.Versioning;
using GACS.IdentityTenant.Application.ApiKeys.Commands;
using GACS.IdentityTenant.Application.ApiKeys.Queries;
using GACS.Shared.Pagination;
using GACS.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GACS.IdentityTenant.Api.Controllers.v1;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/apikeys")]
[Authorize]
public sealed class ApiKeysController : ControllerBase
{
    // TODO: Inject IMessageBus (Wolverine) in constructor

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<object>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByTenant([FromQuery] Guid tenantId, [FromQuery] PaginationParameters pagination, CancellationToken ct)
    {
        // TODO: var result = await _bus.InvokeAsync<ErrorOr<PagedResult<ApiKey>>>(new ListApiKeysQuery(tenantId, pagination), ct);
        throw new NotImplementedException();
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateApiKeyCommand command, CancellationToken ct)
    {
        // TODO: var result = await _bus.InvokeAsync<ErrorOr<CreateApiKeyResult>>(command, ct);
        // Return raw key in response body — ONLY time it is ever visible
        throw new NotImplementedException();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken ct)
    {
        // TODO: var result = await _bus.InvokeAsync<ErrorOr<bool>>(new RevokeApiKeyCommand(id, User.Identity!.Name!), ct);
        throw new NotImplementedException();
    }
}
