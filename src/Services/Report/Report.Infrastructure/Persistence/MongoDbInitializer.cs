using MongoDB.Driver;
using Shared.Common.Entities;

namespace Report.Infrastructure.Persistence;

public class MongoDbInitializer
{
    private readonly MongoDbContext _context;

    public MongoDbInitializer(MongoDbContext context)
    {
        _context = context;
    }

    public async Task InitializeAsync()
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

        await _context.Notifications.Indexes.CreateOneAsync(new CreateIndexModel<Notification>(
            Builders<Notification>.IndexKeys.Ascending(x => x.UserId).Ascending(x => x.IsRead),
            new CreateIndexOptions { Name = "ix_notifications_user_unread" }));
    }
}
