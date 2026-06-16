using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Parking.Domain.Entities;
using Shared.Common.Entities;
using Shared.Common.Settings;

namespace Parking.Infrastructure.Persistence;

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

    public IMongoCollection<Building> Buildings =>
        _database.GetCollection<Building>("buildings");

    public IMongoCollection<Floor> Floors =>
        _database.GetCollection<Floor>("floors");

    public IMongoCollection<Zone> Zones =>
        _database.GetCollection<Zone>("zones");

    public IMongoCollection<VehicleType> VehicleTypes =>
        _database.GetCollection<VehicleType>("vehicle_types");

    public IMongoCollection<Vehicle> Vehicles =>
        _database.GetCollection<Vehicle>("vehicles");

    public IMongoCollection<ParkingSlot> ParkingSlots =>
        _database.GetCollection<ParkingSlot>("parking_slots");

    public IMongoCollection<Gate> Gates =>
        _database.GetCollection<Gate>("gates");

    public IMongoCollection<ParkingSession> ParkingSessions =>
        _database.GetCollection<ParkingSession>("parking_sessions");

    public IMongoCollection<ParkingSessionLog> ParkingSessionLogs =>
        _database.GetCollection<ParkingSessionLog>("parking_session_logs");

    public IMongoCollection<Shift> Shifts =>
        _database.GetCollection<Shift>("shifts");

    public IMongoCollection<IncidentReport> IncidentReports =>
        _database.GetCollection<IncidentReport>("incident_reports");

    public IMongoCollection<Reservation> Reservations =>
        _database.GetCollection<Reservation>("reservations");

    public IMongoCollection<Feedback> Feedbacks =>
        _database.GetCollection<Feedback>("feedbacks");

    public IMongoCollection<AuditLog> AuditLogs =>
        _database.GetCollection<AuditLog>("audit_logs");

    public IMongoCollection<Notification> Notifications =>
        _database.GetCollection<Notification>("notifications");

    public IMongoCollection<T> GetCollection<T>(string name) =>
        _database.GetCollection<T>(name);
}
