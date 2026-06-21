using Auth.Application.Abstractions;
using Auth.Application.Common;
using Auth.Application.DTOs;
using Auth.Application.DTOs.Roles;
using Auth.Domain.Entities;
using Auth.Infrastructure.Persistence;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Auth.Infrastructure.Services;

public class RoleService : IRoleService
{
    /// <summary>
    /// Roles seeded by <see cref="MongoDbInitializer"/>. These underpin the
    /// authorization scheme, so they cannot be renamed via Create or deleted.
    /// </summary>
    private static readonly HashSet<string> SystemRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        MongoDbInitializer.AdminRole,
        MongoDbInitializer.FacilityManagerRole,
        MongoDbInitializer.ParkingStaffRole,
        MongoDbInitializer.DriverRole
    };

    private readonly MongoDbContext _db;

    public RoleService(MongoDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PagedResult<RoleDto>>> GetRolesAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 200) pageSize = 200;

        var filter = Builders<Role>.Filter.Empty;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var regex = new BsonRegularExpression(
                System.Text.RegularExpressions.Regex.Escape(term), "i");

            filter = Builders<Role>.Filter.Or(
                Builders<Role>.Filter.Regex(r => r.Name, regex),
                Builders<Role>.Filter.Regex(r => r.Description, regex));
        }

        var total = await _db.Roles.CountDocumentsAsync(filter, cancellationToken: ct);

        var roles = await _db.Roles
            .Find(filter)
            .SortBy(r => r.Name)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(ct);

        var paged = new PagedResult<RoleDto>
        {
            Items = roles.Select(MapRole).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };

        return Result<PagedResult<RoleDto>>.Ok(paged);
    }

    public async Task<Result<RoleDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var role = await _db.Roles.Find(r => r.Id == id).FirstOrDefaultAsync(ct);
        if (role is null)
            return Result<RoleDto>.Fail("Role not found.", AuthErrorCodes.RoleNotFound);

        return Result<RoleDto>.Ok(MapRole(role));
    }

    public async Task<Result<RoleDto>> CreateAsync(
        CreateRoleRequest request,
        CancellationToken ct = default)
    {
        var name = request.Name.Trim();

        // Name is matched case-insensitively to avoid "admin" vs "Admin" duplicates.
        var existing = await _db.Roles
            .Find(Builders<Role>.Filter.Regex(
                r => r.Name,
                new BsonRegularExpression(
                    $"^{System.Text.RegularExpressions.Regex.Escape(name)}$", "i")))
            .FirstOrDefaultAsync(ct);

        if (existing is not null)
            return Result<RoleDto>.Fail($"Role '{name}' already exists.", AuthErrorCodes.DuplicateRole);

        var role = new Role
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Name = name,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Permissions = NormalizePermissions(request.Permissions),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        try
        {
            await _db.Roles.InsertOneAsync(role, cancellationToken: ct);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return Result<RoleDto>.Fail($"Role '{name}' already exists.", AuthErrorCodes.DuplicateRole);
        }

        return Result<RoleDto>.Ok(MapRole(role));
    }

    public async Task<Result<RoleDto>> UpdateAsync(
        string id,
        UpdateRoleRequest request,
        CancellationToken ct = default)
    {
        var role = await _db.Roles.Find(r => r.Id == id).FirstOrDefaultAsync(ct);
        if (role is null)
            return Result<RoleDto>.Fail("Role not found.", AuthErrorCodes.RoleNotFound);

        // Name is intentionally immutable — users reference roles by name, and the
        // authorization policies depend on the system role names staying stable.
        var update = Builders<Role>.Update
            .Set(r => r.Description, string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim())
            .Set(r => r.Permissions, NormalizePermissions(request.Permissions))
            .Set(r => r.UpdatedAt, DateTime.UtcNow);

        // System roles stay active so authorization keeps working; only custom
        // roles honor the IsActive flag from the request.
        if (!SystemRoles.Contains(role.Name))
            update = update.Set(r => r.IsActive, request.IsActive);

        await _db.Roles.UpdateOneAsync(r => r.Id == id, update, cancellationToken: ct);

        var updated = await _db.Roles.Find(r => r.Id == id).FirstOrDefaultAsync(ct);
        return Result<RoleDto>.Ok(MapRole(updated!));
    }

    public async Task<Result> DeleteAsync(string id, CancellationToken ct = default)
    {
        var role = await _db.Roles.Find(r => r.Id == id).FirstOrDefaultAsync(ct);
        if (role is null)
            return Result.Fail("Role not found.", AuthErrorCodes.RoleNotFound);

        // Deleting a seeded role would break the authorization scheme and lock
        // users out of their permissions, so block it outright.
        if (SystemRoles.Contains(role.Name))
            return Result.Fail(
                $"Role '{role.Name}' is a system role and cannot be deleted.",
                AuthErrorCodes.SystemRoleProtected);

        // Prevent orphaning users: refuse to delete a role still assigned to someone.
        var inUse = await _db.Users
            .Find(Builders<User>.Filter.AnyEq(u => u.Roles, role.Name))
            .AnyAsync(ct);

        if (inUse)
            return Result.Fail(
                $"Role '{role.Name}' is still assigned to one or more users.",
                AuthErrorCodes.ValidationFailed);

        await _db.Roles.DeleteOneAsync(r => r.Id == id, ct);
        return Result.Ok();
    }

    private static List<string> NormalizePermissions(List<string>? permissions) =>
        permissions is null
            ? new List<string>()
            : permissions
                .Select(p => p?.Trim() ?? string.Empty)
                .Where(p => p.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

    private static RoleDto MapRole(Role role) => new()
    {
        Id = role.Id,
        Name = role.Name,
        Description = role.Description,
        Permissions = role.Permissions?.ToList() ?? new List<string>(),
        IsSystem = SystemRoles.Contains(role.Name),
        CreatedAt = role.CreatedAt,
        UpdatedAt = role.UpdatedAt,
        IsActive = role.IsActive
    };
}
