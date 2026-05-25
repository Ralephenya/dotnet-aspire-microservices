using Asp.Versioning;
using GACS.IdentityTenant.Application.Users.Commands;
using GACS.IdentityTenant.Application.Users.Queries;
using GACS.Shared.Pagination;
using GACS.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GACS.IdentityTenant.Api.Controllers.v1;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/users")]
[Authorize]
public sealed class UsersController : ControllerBase
{
    // TODO: Inject IMessageBus (Wolverine) in constructor

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        // TODO: var result = await _bus.InvokeAsync<ErrorOr<User>>(new GetUserQuery(id), ct);
        throw new NotImplementedException();
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateUserCommand command, CancellationToken ct)
    {
        // TODO: var result = await _bus.InvokeAsync<ErrorOr<User>>(command, ct);
        throw new NotImplementedException();
    }
}
