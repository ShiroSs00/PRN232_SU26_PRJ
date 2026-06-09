using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkingSystem.Application.Common;
using ParkingSystem.Application.DTOs;
using ParkingSystem.Application.Services;

namespace ParkingSystem.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;

    public AuthController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login([FromBody] LoginDto loginDto)
    {
        var result = await _userService.AuthenticateAsync(loginDto);
        if (result == null)
        {
            return Unauthorized(ApiResponse.Fail("Invalid email or password."));
        }

        return Ok(ApiResponse.Ok("Login successful", result));
    }

    [HttpPost("logout")]
    public ActionResult<ApiResponse> Logout()
    {
        // Stateless JWT logout is handled on client side by deleting the token.
        // We return a standard success response.
        return Ok(ApiResponse.Ok("Logout successful"));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetMe()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse.Fail("Unauthorized access."));
        }

        var user = await _userService.GetByIdAsync(userId);
        if (user == null)
        {
            return NotFound(ApiResponse.Fail("User profile not found."));
        }

        return Ok(ApiResponse.Ok("Profile loaded successfully", user));
    }
}
