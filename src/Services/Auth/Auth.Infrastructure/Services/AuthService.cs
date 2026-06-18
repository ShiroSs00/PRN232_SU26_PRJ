using Auth.Application.Abstractions;
using Auth.Application.Common;
using Auth.Application.DTOs;
using Auth.Domain.Entities;
using Auth.Infrastructure.Persistence;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Shared.Common.Settings;

namespace Auth.Infrastructure.Services;

public class AuthService : IAuthService
{
    public const string DefaultRole = "Driver";

    private readonly MongoDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;
    private readonly JwtSettings _jwt;

    public AuthService(
        MongoDbContext db,
        IPasswordHasher hasher,
        ITokenService tokens,
        IOptions<JwtSettings> jwt)
    {
        _db = db;
        _hasher = hasher;
        _tokens = tokens;
        _jwt = jwt.Value;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(
        RegisterRequest request,
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
                return Result<AuthResponse>.Fail("Username is already taken.", AuthErrorCodes.DuplicateUsername);
            return Result<AuthResponse>.Fail("Email is already registered.", AuthErrorCodes.DuplicateEmail);
        }

        var user = new User
        {
            Id = ObjectId.GenerateNewId().ToString(),
            FullName = request.FullName.Trim(),
            Username = normalizedUsername,
            Email = normalizedEmail,
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
            PasswordHash = _hasher.Hash(request.Password),
            Roles = new List<string> { DefaultRole },
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        try
        {
            await _db.Users.InsertOneAsync(user, cancellationToken: ct);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            // Race with unique index — translate to a clean error.
            var msg = ex.WriteError?.Message ?? string.Empty;
            return msg.Contains("email", StringComparison.OrdinalIgnoreCase)
                ? Result<AuthResponse>.Fail("Email is already registered.", AuthErrorCodes.DuplicateEmail)
                : Result<AuthResponse>.Fail("Username is already taken.", AuthErrorCodes.DuplicateUsername);
        }

        var response = await IssueTokensAsync(user, ct);
        return Result<AuthResponse>.Ok(response);
    }

    public async Task<Result<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken ct = default)
    {
        var key = request.UsernameOrEmail.Trim().ToLowerInvariant();

        var user = await _db.Users
            .Find(u => u.Username == key || u.Email == key)
            .FirstOrDefaultAsync(ct);

        if (user is null || !_hasher.Verify(request.Password, user.PasswordHash))
            return Result<AuthResponse>.Fail("Invalid credentials.", AuthErrorCodes.InvalidCredentials);

        if (!user.IsActive)
            return Result<AuthResponse>.Fail("Account is disabled.", AuthErrorCodes.AccountInactive);

        user.LastLoginAt = DateTime.UtcNow;
        var update = Builders<User>.Update
            .Set(u => u.LastLoginAt, user.LastLoginAt)
            .Set(u => u.UpdatedAt, DateTime.UtcNow);
        await _db.Users.UpdateOneAsync(u => u.Id == user.Id, update, cancellationToken: ct);

        var response = await IssueTokensAsync(user, ct);
        return Result<AuthResponse>.Ok(response);
    }

    public async Task<Result> ChangePasswordAsync(
        string userId,
        ChangePasswordRequest request,
        CancellationToken ct = default)
    {
        var user = await _db.Users.Find(u => u.Id == userId).FirstOrDefaultAsync(ct);
        if (user is null)
            return Result.Fail("User not found.", AuthErrorCodes.UserNotFound);

        if (!_hasher.Verify(request.CurrentPassword, user.PasswordHash))
            return Result.Fail("Current password is incorrect.", AuthErrorCodes.InvalidCurrentPassword);

        var newHash = _hasher.Hash(request.NewPassword);
        var update = Builders<User>.Update
            .Set(u => u.PasswordHash, newHash)
            .Set(u => u.UpdatedAt, DateTime.UtcNow);
        await _db.Users.UpdateOneAsync(u => u.Id == userId, update, cancellationToken: ct);

        // Revoke all existing refresh tokens for this user — force re-login on other devices.
        var revoke = Builders<RefreshToken>.Update.Set(r => r.IsRevoked, true);
        await _db.RefreshTokens.UpdateManyAsync(
            r => r.UserId == userId && !r.IsRevoked,
            revoke,
            cancellationToken: ct);

        return Result.Ok();
    }

    public async Task<Result<AuthResponse>> RefreshTokenAsync(
        RefreshTokenRequest request,
        CancellationToken ct = default)
    {
        var stored = await _db.RefreshTokens
            .Find(r => r.Token == request.RefreshToken)
            .FirstOrDefaultAsync(ct);

        if (stored is null || stored.IsRevoked || stored.ExpiresAt < DateTime.UtcNow)
            return Result<AuthResponse>.Fail("Invalid or expired refresh token.", AuthErrorCodes.InvalidRefreshToken);

        var user = await _db.Users.Find(u => u.Id == stored.UserId).FirstOrDefaultAsync(ct);
        if (user is null || !user.IsActive)
            return Result<AuthResponse>.Fail("User no longer available.", AuthErrorCodes.UserNotFound);

        // Rotate: revoke the old, issue a new pair.
        var revoke = Builders<RefreshToken>.Update.Set(r => r.IsRevoked, true);
        await _db.RefreshTokens.UpdateOneAsync(r => r.Id == stored.Id, revoke, cancellationToken: ct);

        var response = await IssueTokensAsync(user, ct);
        return Result<AuthResponse>.Ok(response);
    }

    public async Task<Result<UserDto>> GetCurrentUserAsync(string userId, CancellationToken ct = default)
    {
        var user = await _db.Users.Find(u => u.Id == userId).FirstOrDefaultAsync(ct);
        if (user is null)
            return Result<UserDto>.Fail("User not found.", AuthErrorCodes.UserNotFound);

        return Result<UserDto>.Ok(MapUser(user));
    }

    public async Task<Result> LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        var update = Builders<RefreshToken>.Update.Set(r => r.IsRevoked, true);
        var result = await _db.RefreshTokens.UpdateOneAsync(
            r => r.Token == refreshToken,
            update,
            cancellationToken: ct);

        // Idempotent: even if the token isn't found we treat it as success.
        return Result.Ok();
    }

    private async Task<AuthResponse> IssueTokensAsync(User user, CancellationToken ct)
    {
        var (accessToken, expiresAt) = _tokens.GenerateAccessToken(user);
        var refreshTokenValue = _tokens.GenerateRefreshToken();

        var refreshDoc = new RefreshToken
        {
            Id = ObjectId.GenerateNewId().ToString(),
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwt.RefreshExpirationInDays),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        await _db.RefreshTokens.InsertOneAsync(refreshDoc, cancellationToken: ct);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            TokenType = "Bearer",
            ExpiresAt = expiresAt,
            User = MapUser(user)
        };
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
