using System.Security.Claims;
using Auth.Application.Abstractions;
using Auth.Application.Common;
using Auth.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken ct)
    {
        var result = await _auth.RegisterAsync(request, ct);
        if (!result.Success)
        {
            var status = result.ErrorCode switch
            {
                AuthErrorCodes.DuplicateUsername => StatusCodes.Status409Conflict,
                AuthErrorCodes.DuplicateEmail => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest
            };
            return StatusCode(status, ApiResponse.Fail(result.Error!));
        }

        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse<AuthResponse>.Ok(result.Value!, "Registration successful."));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken ct)
    {
        var result = await _auth.LoginAsync(request, ct);
        if (!result.Success)
        {
            var status = result.ErrorCode switch
            {
                AuthErrorCodes.InvalidCredentials => StatusCodes.Status401Unauthorized,
                AuthErrorCodes.AccountInactive => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status400BadRequest
            };
            return StatusCode(status, ApiResponse.Fail(result.Error!));
        }

        return Ok(ApiResponse<AuthResponse>.Ok(result.Value!, "Login successful."));
    }

    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken ct)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var result = await _auth.ChangePasswordAsync(userId, request, ct);
        if (!result.Success)
        {
            var status = result.ErrorCode switch
            {
                AuthErrorCodes.InvalidCurrentPassword => StatusCodes.Status400BadRequest,
                AuthErrorCodes.UserNotFound => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status400BadRequest
            };
            return StatusCode(status, ApiResponse.Fail(result.Error!));
        }

        return Ok(ApiResponse.Ok("Password updated."));
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken(
        [FromBody] RefreshTokenRequest request,
        CancellationToken ct)
    {
        var result = await _auth.RefreshTokenAsync(request, ct);
        if (!result.Success)
        {
            return Unauthorized(ApiResponse.Fail(result.Error!));
        }

        return Ok(ApiResponse<AuthResponse>.Ok(result.Value!, "Token refreshed."));
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var result = await _auth.GetCurrentUserAsync(userId, ct);
        if (!result.Success)
            return NotFound(ApiResponse.Fail(result.Error!));

        return Ok(ApiResponse<UserDto>.Ok(result.Value!));
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(
        [FromBody] RefreshTokenRequest request,
        CancellationToken ct)
    {
        await _auth.LogoutAsync(request.RefreshToken, ct);
        return Ok(ApiResponse.Ok("Logged out."));
    }

    private string? GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ??
        User.FindFirstValue("sub");
}
