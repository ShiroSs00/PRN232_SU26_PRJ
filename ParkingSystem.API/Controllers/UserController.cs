using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkingSystem.Application.Common;
using ParkingSystem.Application.DTOs;
using ParkingSystem.Application.Services;
using ParkingSystem.Domain.Constants;

namespace ParkingSystem.API.Controllers;

[ApiController]
[Route("api/v1/users")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserDto>>>> GetAll()
    {
        var users = await _userService.GetAllAsync();
        return Ok(ApiResponse.Ok("Users retrieved successfully", users));
    }

    [Authorize]
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetById(string id)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isAdmin = User.IsInRole(UserRoles.Admin);

        if (currentUserId != id && !isAdmin)
        {
            return Forbid();
        }

        var user = await _userService.GetByIdAsync(id);
        if (user == null)
        {
            return NotFound(ApiResponse.Fail("User not found."));
        }

        return Ok(ApiResponse.Ok("User retrieved successfully", user));
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserDto>>> Create([FromBody] CreateUserDto createUserDto)
    {
        try
        {
            var user = await _userService.CreateAsync(createUserDto);
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, ApiResponse.Ok("User created successfully", user));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Update(string id, [FromBody] UpdateUserDto updateUserDto)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isAdmin = User.IsInRole(UserRoles.Admin);

        if (currentUserId != id && !isAdmin)
        {
            return Forbid();
        }

        // Non-admin cannot change roles or isActive status
        if (!isAdmin)
        {
            updateUserDto.Roles = []; // Clear role modifications
            updateUserDto.IsActive = null; // Prevent status change
        }

        try
        {
            var user = await _userService.UpdateAsync(id, updateUserDto);
            if (user == null)
            {
                return NotFound(ApiResponse.Fail("User not found."));
            }

            return Ok(ApiResponse.Ok("User updated successfully", user));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> Delete(string id)
    {
        var deleted = await _userService.DeleteAsync(id);
        if (!deleted)
        {
            return NotFound(ApiResponse.Fail("User not found or already deleted."));
        }

        return Ok(ApiResponse.Ok("User soft deleted successfully"));
    }
}
