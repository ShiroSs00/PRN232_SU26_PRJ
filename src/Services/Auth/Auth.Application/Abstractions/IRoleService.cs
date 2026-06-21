using Auth.Application.Common;
using Auth.Application.DTOs;
using Auth.Application.DTOs.Roles;

namespace Auth.Application.Abstractions;

public interface IRoleService
{
    Task<Result<PagedResult<RoleDto>>> GetRolesAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken ct = default);

    Task<Result<RoleDto>> GetByIdAsync(string id, CancellationToken ct = default);

    Task<Result<RoleDto>> CreateAsync(CreateRoleRequest request, CancellationToken ct = default);

    Task<Result<RoleDto>> UpdateAsync(string id, UpdateRoleRequest request, CancellationToken ct = default);

    Task<Result> DeleteAsync(string id, CancellationToken ct = default);
}
