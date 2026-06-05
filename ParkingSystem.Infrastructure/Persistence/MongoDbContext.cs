using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ParkingSystem.Domain.Entities;
using ParkingSystem.Infrastructure.Settings;

namespace ParkingSystem.Infrastructure.Persistence;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoDbSettings> options)
    {
        var settings = options.Value;
        var client = new MongoClient(settings.ConnectionString);
        _database = client.GetDatabase(settings.DatabaseName);
        DatabaseName = settings.DatabaseName;
    }

    public string DatabaseName { get; }

    public IMongoDatabase Database => _database;

    public IMongoCollection<User> Users => _database.GetCollection<User>("users");

    public IMongoCollection<Role> Roles => _database.GetCollection<Role>("roles");

    public IMongoCollection<Building> Buildings => _database.GetCollection<Building>("buildings");

    public IMongoCollection<Floor> Floors => _database.GetCollection<Floor>("floors");

    public IMongoCollection<Zone> Zones => _database.GetCollection<Zone>("zones");

    public IMongoCollection<VehicleType> VehicleTypes => _database.GetCollection<VehicleType>("vehicleTypes");

    public IMongoCollection<ParkingSlot> ParkingSlots => _database.GetCollection<ParkingSlot>("parkingSlots");

    public IMongoCollection<FeePolicy> FeePolicies => _database.GetCollection<FeePolicy>("feePolicies");

    public IMongoCollection<ParkingSession> ParkingSessions => _database.GetCollection<ParkingSession>("parkingSessions");

    public IMongoCollection<Payment> Payments => _database.GetCollection<Payment>("payments");

    public IMongoCollection<AuditLog> AuditLogs => _database.GetCollection<AuditLog>("auditLogs");
}
