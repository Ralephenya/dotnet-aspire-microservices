using Asp.Versioning;
using GACS.IdentityTenant.Application.Tenants.Commands;
using GACS.IdentityTenant.Application.Tenants.Queries;
using GACS.Shared.Pagination;
using GACS.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GACS.IdentityTenant.Api.Controllers.v1;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/tenants")]
[Authorize]
public sealed class TenantsController : ControllerBase
{
    // TODO: Inject IMessageBus (Wolverine) in constructor

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<object>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParameters pagination, CancellationToken ct)
    {
        // TODO: var result = await _bus.InvokeAsync<ErrorOr<PagedResult<Tenant>>>(new ListTenantsQuery(pagination), ct);
        // return result.Match(data => Ok(ApiResponse<PagedResult<Tenant>>.Ok(data)), errors => errors.ToProblemResult());
        throw new NotImplementedException();
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        // TODO: var result = await _bus.InvokeAsync<ErrorOr<Tenant>>(new GetTenantQuery(id), ct);
        throw new NotImplementedException();
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateTenantCommand command, CancellationToken ct)
    {
        // TODO: var result = await _bus.InvokeAsync<ErrorOr<Tenant>>(command, ct);
        throw new NotImplementedException();
    }
}
