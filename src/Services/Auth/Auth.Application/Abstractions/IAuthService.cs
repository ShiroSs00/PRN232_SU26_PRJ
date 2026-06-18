using Auth.Application.Common;
using Auth.Application.DTOs;

namespace Auth.Application.Abstractions;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);

    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);

    Task<Result> ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken ct = default);

    Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default);

    Task<Result<UserDto>> GetCurrentUserAsync(string userId, CancellationToken ct = default);

    Task<Result> LogoutAsync(string refreshToken, CancellationToken ct = default);
}
