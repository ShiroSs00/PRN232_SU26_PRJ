using Auth.Domain.Entities;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Shared.Common.Entities;
using Shared.Common.Settings;

namespace Auth.Infrastructure.Persistence;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        _database = client.GetDatabase(settings.Value.DatabaseName);
    }

    public string DatabaseName => _database.DatabaseNamespace.DatabaseName;

    public IMongoDatabase Database => _database;

    public IMongoCollection<User> Users =>
        _database.GetCollection<User>("users");

    public IMongoCollection<Role> Roles =>
        _database.GetCollection<Role>("roles");

    public IMongoCollection<RefreshToken> RefreshTokens =>
        _database.GetCollection<RefreshToken>("refresh_tokens");

    public IMongoCollection<AuditLog> AuditLogs =>
        _database.GetCollection<AuditLog>("audit_logs");

    public IMongoCollection<Notification> Notifications =>
        _database.GetCollection<Notification>("notifications");

    public IMongoCollection<T> GetCollection<T>(string name) =>
        _database.GetCollection<T>(name);
}
