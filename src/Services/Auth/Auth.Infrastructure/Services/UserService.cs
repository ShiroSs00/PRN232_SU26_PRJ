using Auth.Application.Abstractions;
using Auth.Application.Common;
using Auth.Application.DTOs;
using Auth.Domain.Entities;
using Auth.Infrastructure.Persistence;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Auth.Infrastructure.Services;

public class UserService : IUserService
{
    public const string DefaultRole = "Driver";

    private readonly MongoDbContext _db;
    private readonly IPasswordHasher _hasher;

    public UserService(MongoDbContext db, IPasswordHasher hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    public async Task<Result<PagedResult<UserDto>>> GetUsersAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 200) pageSize = 200;

        var filter = Builders<User>.Filter.Empty;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var regex = new MongoDB.Bson.BsonRegularExpression(
                System.Text.RegularExpressions.Regex.Escape(term), "i");

            filter = Builders<User>.Filter.Or(
                Builders<User>.Filter.Regex(u => u.Username, regex),
                Builders<User>.Filter.Regex(u => u.Email, regex),
                Builders<User>.Filter.Regex(u => u.FullName, regex));
        }

        var total = await _db.Users.CountDocumentsAsync(filter, cancellationToken: ct);

        var users = await _db.Users
            .Find(filter)
            .SortByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(ct);

        var paged = new PagedResult<UserDto>
        {
            Items = users.Select(MapUser).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };

        return Result<PagedResult<UserDto>>.Ok(paged);
    }

    public async Task<Result<UserDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var user = await _db.Users.Find(u => u.Id == id).FirstOrDefaultAsync(ct);
        if (user is null)
            return Result<UserDto>.Fail("User not found.", AuthErrorCodes.UserNotFound);

        return Result<UserDto>.Ok(MapUser(user));
    }

    public async Task<Result<UserDto>> CreateAsync(
        CreateUserRequest request,
        CancellationToken ct = default)
    {
        var normalizedUsername = request.Username.Trim().ToLowerInvariant();
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var existing = await _db.Users
            .Find(u => u.Username == normalizedUsername || u.Email == normalizedEmail)
            .FirstOrDefaultAsync(ct);

        if (existing is not null)
        {
            if (existing.Username == normalizedUsername)
                return Result<UserDto>.Fail("Username is already taken.", AuthErrorCodes.DuplicateUsername);
            return Result<UserDto>.Fail("Email is already registered.", AuthErrorCodes.DuplicateEmail);
        }

        var requestedRoles = (request.Roles is null || request.Roles.Count == 0)
            ? new List<string> { DefaultRole }
            : request.Roles.Select(r => r.Trim()).Where(r => r.Length > 0).Distinct().ToList();

        var roleValidation = await ValidateRolesAsync(requestedRoles, ct);
        if (!roleValidation.Success)
            return Result<UserDto>.Fail(roleValidation.Error!, roleValidation.ErrorCode!);

        var user = new User
        {
            Id = ObjectId.GenerateNewId().ToString(),
            FullName = request.FullName.Trim(),
            Username = normalizedUsername,
            Email = normalizedEmail,
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
            AvatarUrl = string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl.Trim(),
            PasswordHash = _hasher.Hash(request.Password),
            Roles = roleValidation.Value!,
            CreatedAt = DateTime.UtcNow,
            IsActive = request.IsActive
        };

        try
        {
            await _db.Users.InsertOneAsync(user, cancellationToken: ct);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            var msg = ex.WriteError?.Message ?? string.Empty;
            return msg.Contains("email", StringComparison.OrdinalIgnoreCase)
                ? Result<UserDto>.Fail("Email is already registered.", AuthErrorCodes.DuplicateEmail)
                : Result<UserDto>.Fail("Username is already taken.", AuthErrorCodes.DuplicateUsername);
        }

        return Result<UserDto>.Ok(MapUser(user));
    }

    public async Task<Result<UserDto>> UpdateAsync(
        string id,
        UpdateUserRequest request,
        CancellationToken ct = default)
    {
        var user = await _db.Users.Find(u => u.Id == id).FirstOrDefaultAsync(ct);
        if (user is null)
            return Result<UserDto>.Fail("User not found.", AuthErrorCodes.UserNotFound);

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (!string.Equals(user.Email, normalizedEmail, StringComparison.Ordinal))
        {
            var emailOwner = await _db.Users
                .Find(u => u.Email == normalizedEmail && u.Id != id)
                .FirstOrDefaultAsync(ct);

            if (emailOwner is not null)
                return Result<UserDto>.Fail("Email is already registered.", AuthErrorCodes.DuplicateEmail);
        }

        var requestedRoles = request.Roles
            .Select(r => r?.Trim() ?? string.Empty)
            .Where(r => r.Length > 0)
            .Distinct()
            .ToList();

        if (requestedRoles.Count == 0)
            return Result<UserDto>.Fail("At least one role is required.", AuthErrorCodes.ValidationFailed);

        var roleValidation = await ValidateRolesAsync(requestedRoles, ct);
        if (!roleValidation.Success)
            return Result<UserDto>.Fail(roleValidation.Error!, roleValidation.ErrorCode!);

        var update = Builders<User>.Update
            .Set(u => u.FullName, request.FullName.Trim())
            .Set(u => u.Email, normalizedEmail)
            .Set(u => u.PhoneNumber, string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim())
            .Set(u => u.AvatarUrl, string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl.Trim())
            .Set(u => u.Roles, roleValidation.Value!)
            .Set(u => u.IsActive, request.IsActive)
            .Set(u => u.UpdatedAt, DateTime.UtcNow);

        try
        {
            await _db.Users.UpdateOneAsync(u => u.Id == id, update, cancellationToken: ct);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return Result<UserDto>.Fail("Email is already registered.", AuthErrorCodes.DuplicateEmail);
        }

        var updated = await _db.Users.Find(u => u.Id == id).FirstOrDefaultAsync(ct);
        return Result<UserDto>.Ok(MapUser(updated!));
    }

    public async Task<Result> DeleteAsync(string id, CancellationToken ct = default)
    {
        var user = await _db.Users.Find(u => u.Id == id).FirstOrDefaultAsync(ct);
        if (user is null)
            return Result.Fail("User not found.", AuthErrorCodes.UserNotFound);

        var update = Builders<User>.Update
            .Set(u => u.IsActive, false)
            .Set(u => u.UpdatedAt, DateTime.UtcNow);

        await _db.Users.UpdateOneAsync(u => u.Id == id, update, cancellationToken: ct);

        // Revoke all active refresh tokens — disabled users should be locked out.
        var revoke = Builders<RefreshToken>.Update.Set(r => r.IsRevoked, true);
        await _db.RefreshTokens.UpdateManyAsync(
            r => r.UserId == id && !r.IsRevoked,
            revoke,
            cancellationToken: ct);

        return Result.Ok();
    }

    private async Task<Result<List<string>>> ValidateRolesAsync(
        List<string> requested,
        CancellationToken ct)
    {
        var existingRoles = await _db.Roles
            .Find(Builders<Role>.Filter.In(r => r.Name, requested))
            .ToListAsync(ct);

        var existingNames = existingRoles
            .Select(r => r.Name)
            .ToHashSet(StringComparer.Ordinal);

        var missing = requested.Where(r => !existingNames.Contains(r)).ToList();
        if (missing.Count > 0)
        {
            return Result<List<string>>.Fail(
                $"Unknown role(s): {string.Join(", ", missing)}.",
                AuthErrorCodes.InvalidRole);
        }

        // Preserve the canonical casing stored in the roles collection.
        return Result<List<string>>.Ok(existingRoles.Select(r => r.Name).Distinct().ToList());
    }

    private static UserDto MapUser(User user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Username = user.Username,
        Email = user.Email,
        PhoneNumber = user.PhoneNumber,
        AvatarUrl = user.AvatarUrl,
        LastLoginAt = user.LastLoginAt,
        CreatedAt = user.CreatedAt,
        IsActive = user.IsActive,
        Roles = user.Roles?.ToList() ?? new List<string>()
    };
}
