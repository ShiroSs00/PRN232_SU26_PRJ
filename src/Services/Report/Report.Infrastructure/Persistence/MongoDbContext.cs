using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Report.Application.Settings;
using Report.Infrastructure.ReadModels;
using Shared.Common.Entities;
using Shared.Common.Settings;

namespace Report.Infrastructure.Persistence;

/// <summary>
/// Report reads from its own database plus sibling service databases on the
/// same Atlas cluster (read-only) to aggregate revenue/occupancy/flow reports.
/// </summary>
public class MongoDbContext
{
    private readonly IMongoDatabase _database;
    private readonly IMongoDatabase _paymentDb;
    private readonly IMongoDatabase _parkingDb;

    public MongoDbContext(
        IOptions<MongoDbSettings> settings,
        IOptions<ReportSourceSettings> sources)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        _database = client.GetDatabase(settings.Value.DatabaseName);
        _paymentDb = client.GetDatabase(sources.Value.PaymentDatabaseName);
        _parkingDb = client.GetDatabase(sources.Value.ParkingDatabaseName);
    }

    public string DatabaseName => _database.DatabaseNamespace.DatabaseName;

    public IMongoDatabase Database => _database;

    // Own database collections.
    public IMongoCollection<AuditLog> AuditLogs =>
        _database.GetCollection<AuditLog>("audit_logs");

    public IMongoCollection<Notification> Notifications =>
        _database.GetCollection<Notification>("notifications");

    // Read-only views into the payment service database.
    public IMongoCollection<PaymentReadModel> Payments =>
        _paymentDb.GetCollection<PaymentReadModel>("payments");

    public IMongoCollection<SubscriptionReadModel> Subscriptions =>
        _paymentDb.GetCollection<SubscriptionReadModel>("subscriptions");

    // Read-only views into the parking service database.
    public IMongoCollection<ParkingSlotReadModel> ParkingSlots =>
        _parkingDb.GetCollection<ParkingSlotReadModel>("parking_slots");

    public IMongoCollection<ParkingSessionReadModel> ParkingSessions =>
        _parkingDb.GetCollection<ParkingSessionReadModel>("parking_sessions");

    public IMongoCollection<ShiftReadModel> Shifts =>
        _parkingDb.GetCollection<ShiftReadModel>("shifts");

    public IMongoCollection<T> GetCollection<T>(string name) =>
        _database.GetCollection<T>(name);
}
