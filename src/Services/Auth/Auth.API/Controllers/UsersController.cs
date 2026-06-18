using Auth.Application.Abstractions;
using Auth.Application.Common;
using Auth.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth.API.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly IUserService _users;

    public UsersController(IUserService users)
    {
        _users = users;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<UserDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var result = await _users.GetUsersAsync(page, pageSize, search, ct);
        return Ok(ApiResponse<PagedResult<UserDto>>.Ok(result.Value!));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _users.GetByIdAsync(id, ct);
        if (!result.Success)
            return NotFound(ApiResponse.Fail(result.Error!));

        return Ok(ApiResponse<UserDto>.Ok(result.Value!));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken ct)
    {
        var result = await _users.CreateAsync(request, ct);
        if (!result.Success)
        {
            var status = result.ErrorCode switch
            {
                AuthErrorCodes.DuplicateUsername => StatusCodes.Status409Conflict,
                AuthErrorCodes.DuplicateEmail => StatusCodes.Status409Conflict,
                AuthErrorCodes.InvalidRole => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status400BadRequest
            };
            return StatusCode(status, ApiResponse.Fail(result.Error!));
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value!.Id },
            ApiResponse<UserDto>.Ok(result.Value!, "User created."));
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(
        string id,
        [FromBody] UpdateUserRequest request,
        CancellationToken ct)
    {
        var result = await _users.UpdateAsync(id, request, ct);
        if (!result.Success)
        {
            var status = result.ErrorCode switch
            {
                AuthErrorCodes.UserNotFound => StatusCodes.Status404NotFound,
                AuthErrorCodes.DuplicateEmail => StatusCodes.Status409Conflict,
                AuthErrorCodes.InvalidRole => StatusCodes.Status400BadRequest,
                AuthErrorCodes.ValidationFailed => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status400BadRequest
            };
            return StatusCode(status, ApiResponse.Fail(result.Error!));
        }

        return Ok(ApiResponse<UserDto>.Ok(result.Value!, "User updated."));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await _users.DeleteAsync(id, ct);
        if (!result.Success)
        {
            var status = result.ErrorCode switch
            {
                AuthErrorCodes.UserNotFound => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status400BadRequest
            };
            return StatusCode(status, ApiResponse.Fail(result.Error!));
        }

        return Ok(ApiResponse.Ok("User deactivated."));
    }
}
