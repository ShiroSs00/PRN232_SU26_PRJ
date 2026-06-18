using Auth.Application.Abstractions;
using Auth.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Driver;
using Shared.Common.Entities;

namespace Auth.Infrastructure.Persistence;

public class MongoDbInitializer
{
    public const string AdminRole = "Admin";
    public const string FacilityManagerRole = "FacilityManager";
    public const string ParkingStaffRole = "ParkingStaff";
    public const string DriverRole = "Driver";

    public const string DefaultAdminUsername = "admin";
    public const string DefaultAdminEmail = "admin@parking.local";
    public const string DefaultAdminPassword = "Admin@123";

    private readonly MongoDbContext _context;
    private readonly IPasswordHasher _hasher;

    public MongoDbInitializer(MongoDbContext context, IPasswordHasher hasher)
    {
        _context = context;
        _hasher = hasher;
    }

    public async Task InitializeAsync()
    {
        await CreateUserIndexesAsync();
        await CreateRoleIndexesAsync();
        await CreateRefreshTokenIndexesAsync();
        await CreateSharedIndexesAsync();
        await SeedRolesAsync();
        await SeedAdminUserAsync();
    }

    private async Task CreateUserIndexesAsync()
    {
        var indexes = new[]
        {
            new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(x => x.Username),
                new CreateIndexOptions { Unique = true, Name = "ux_users_username" }),
            new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(x => x.Email),
                new CreateIndexOptions { Unique = true, Name = "ux_users_email" }),
            new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(x => x.IsActive),
                new CreateIndexOptions { Name = "ix_users_is_active" })
        };

        await _context.Users.Indexes.CreateManyAsync(indexes);
    }

    private async Task CreateRoleIndexesAsync()
    {
        var indexes = new[]
        {
            new CreateIndexModel<Role>(
                Builders<Role>.IndexKeys.Ascending(x => x.Name),
                new CreateIndexOptions { Unique = true, Name = "ux_roles_name" })
        };

        await _context.Roles.Indexes.CreateManyAsync(indexes);
    }

    private async Task CreateRefreshTokenIndexesAsync()
    {
        var indexes = new[]
        {
            new CreateIndexModel<RefreshToken>(
                Builders<RefreshToken>.IndexKeys.Ascending(x => x.Token),
                new CreateIndexOptions { Unique = true, Name = "ux_refresh_tokens_token" }),
            new CreateIndexModel<RefreshToken>(
                Builders<RefreshToken>.IndexKeys.Ascending(x => x.UserId),
                new CreateIndexOptions { Name = "ix_refresh_tokens_user_id" }),
            new CreateIndexModel<RefreshToken>(
                Builders<RefreshToken>.IndexKeys.Ascending(x => x.ExpiresAt),
                new CreateIndexOptions { Name = "ix_refresh_tokens_expires_at" })
        };

        await _context.RefreshTokens.Indexes.CreateManyAsync(indexes);
    }

    private async Task CreateSharedIndexesAsync()
    {
        await _context.AuditLogs.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<AuditLog>(
                Builders<AuditLog>.IndexKeys.Ascending(x => x.EntityName).Ascending(x => x.EntityId),
                new CreateIndexOptions { Name = "ix_audit_logs_entity" }),
            new CreateIndexModel<AuditLog>(
                Builders<AuditLog>.IndexKeys.Descending(x => x.CreatedAt),
                new CreateIndexOptions { Name = "ix_audit_logs_created_at" })
        });

        await _context.Notifications.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<Notification>(
                Builders<Notification>.IndexKeys.Ascending(x => x.UserId).Ascending(x => x.IsRead),
                new CreateIndexOptions { Name = "ix_notifications_user_unread" })
        });
    }

    private async Task SeedRolesAsync()
    {
        var defaults = new (string Name, string Description)[]
        {
            (AdminRole, "Full system administrator."),
            (FacilityManagerRole, "Manages parking facilities and staff."),
            (ParkingStaffRole, "Operates check-in/check-out at the gate."),
            (DriverRole, "End-user driver who parks vehicles.")
        };

        foreach (var (name, description) in defaults)
        {
            var existing = await _context.Roles.Find(r => r.Name == name).FirstOrDefaultAsync();
            if (existing is not null) continue;

            var role = new Role
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Name = name,
                Description = description,
                Permissions = new List<string>(),
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            try
            {
                await _context.Roles.InsertOneAsync(role);
            }
            catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
            {
                // Another instance got there first — fine.
            }
        }
    }

    private async Task SeedAdminUserAsync()
    {
        var existing = await _context.Users
            .Find(u => u.Username == DefaultAdminUsername || u.Email == DefaultAdminEmail)
            .FirstOrDefaultAsync();

        if (existing is not null) return;

        var admin = new User
        {
            Id = ObjectId.GenerateNewId().ToString(),
            FullName = "System Administrator",
            Username = DefaultAdminUsername,
            Email = DefaultAdminEmail,
            PasswordHash = _hasher.Hash(DefaultAdminPassword),
            Roles = new List<string> { AdminRole },
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        try
        {
            await _context.Users.InsertOneAsync(admin);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            // Race with another startup — admin already exists.
        }
    }
}
