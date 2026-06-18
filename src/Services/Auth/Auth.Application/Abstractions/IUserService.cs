using Auth.Application.Common;
using Auth.Application.DTOs;

namespace Auth.Application.Abstractions;

public interface IUserService
{
    Task<Result<PagedResult<UserDto>>> GetUsersAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken ct = default);

    Task<Result<UserDto>> GetByIdAsync(string id, CancellationToken ct = default);

    Task<Result<UserDto>> CreateAsync(CreateUserRequest request, CancellationToken ct = default);

    Task<Result<UserDto>> UpdateAsync(string id, UpdateUserRequest request, CancellationToken ct = default);

    Task<Result> DeleteAsync(string id, CancellationToken ct = default);
}
