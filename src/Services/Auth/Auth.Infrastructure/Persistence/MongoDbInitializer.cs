using Auth.Domain.Entities;
using MongoDB.Driver;
using Shared.Common.Entities;

namespace Auth.Infrastructure.Persistence;

public class MongoDbInitializer
{
    private readonly MongoDbContext _context;

    public MongoDbInitializer(MongoDbContext context)
    {
        _context = context;
    }

    public async Task InitializeAsync()
    {
        await CreateUserIndexesAsync();
        await CreateRoleIndexesAsync();
        await CreateRefreshTokenIndexesAsync();
        await CreateSharedIndexesAsync();
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
}
