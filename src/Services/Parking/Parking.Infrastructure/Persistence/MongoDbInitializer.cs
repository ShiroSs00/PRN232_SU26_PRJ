using MongoDB.Driver;
using Parking.Domain.Entities;
using Shared.Common.Entities;

namespace Parking.Infrastructure.Persistence;

public class MongoDbInitializer
{
    private readonly MongoDbContext _context;

    public MongoDbInitializer(MongoDbContext context)
    {
        _context = context;
    }

    public async Task InitializeAsync()
    {
        await CreateStructureIndexesAsync();
        await CreateVehicleIndexesAsync();
        await CreateSlotAndSessionIndexesAsync();
        await CreateOperationIndexesAsync();
        await CreateSharedIndexesAsync();
    }

    private async Task CreateStructureIndexesAsync()
    {
        await _context.Buildings.Indexes.CreateOneAsync(new CreateIndexModel<Building>(
            Builders<Building>.IndexKeys.Ascending(x => x.Name),
            new CreateIndexOptions { Name = "ix_buildings_name" }));

        await _context.Floors.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<Floor>(
                Builders<Floor>.IndexKeys.Ascending(x => x.BuildingId),
                new CreateIndexOptions { Name = "ix_floors_building_id" })
        });

        await _context.Zones.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<Zone>(
                Builders<Zone>.IndexKeys.Ascending(x => x.BuildingId).Ascending(x => x.FloorId),
                new CreateIndexOptions { Name = "ix_zones_building_floor" }),
            new CreateIndexModel<Zone>(
                Builders<Zone>.IndexKeys.Ascending(x => x.VehicleTypeId),
                new CreateIndexOptions { Name = "ix_zones_vehicle_type_id" })
        });

        await _context.VehicleTypes.Indexes.CreateOneAsync(new CreateIndexModel<VehicleType>(
            Builders<VehicleType>.IndexKeys.Ascending(x => x.Name),
            new CreateIndexOptions { Unique = true, Name = "ux_vehicle_types_name" }));
    }

    private async Task CreateVehicleIndexesAsync()
    {
        await _context.Vehicles.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<Vehicle>(
                Builders<Vehicle>.IndexKeys.Ascending(x => x.PlateNumberNormalized),
                new CreateIndexOptions { Unique = true, Name = "ux_vehicles_plate_number_normalized" }),
            new CreateIndexModel<Vehicle>(
                Builders<Vehicle>.IndexKeys.Ascending(x => x.OwnerUserId),
                new CreateIndexOptions { Name = "ix_vehicles_owner_user_id" }),
            new CreateIndexModel<Vehicle>(
                Builders<Vehicle>.IndexKeys.Ascending(x => x.VehicleTypeId),
                new CreateIndexOptions { Name = "ix_vehicles_vehicle_type_id" })
        });
    }

    private async Task CreateSlotAndSessionIndexesAsync()
    {
        await _context.ParkingSlots.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<ParkingSlot>(
                Builders<ParkingSlot>.IndexKeys.Ascending(x => x.Code),
                new CreateIndexOptions { Unique = true, Name = "ux_parking_slots_code" }),
            new CreateIndexModel<ParkingSlot>(
                Builders<ParkingSlot>.IndexKeys.Ascending(x => x.BuildingId).Ascending(x => x.ZoneId).Ascending(x => x.Status),
                new CreateIndexOptions { Name = "ix_parking_slots_availability" }),
            new CreateIndexModel<ParkingSlot>(
                Builders<ParkingSlot>.IndexKeys.Ascending(x => x.CurrentSessionId),
                new CreateIndexOptions { Name = "ix_parking_slots_current_session_id" })
        });

        await _context.Gates.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<Gate>(
                Builders<Gate>.IndexKeys.Ascending(x => x.BuildingId).Ascending(x => x.Code),
                new CreateIndexOptions { Unique = true, Name = "ux_gates_building_code" })
        });

        await _context.ParkingSessions.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<ParkingSession>(
                Builders<ParkingSession>.IndexKeys.Ascending(x => x.PlateNumber).Ascending(x => x.Status),
                new CreateIndexOptions { Name = "ix_parking_sessions_plate_status" }),
            new CreateIndexModel<ParkingSession>(
                Builders<ParkingSession>.IndexKeys.Ascending(x => x.PlateNumber).Ascending(x => x.Status),
                new CreateIndexOptions<ParkingSession>
                {
                    Name = "ux_parking_sessions_active_plate",
                    Unique = true,
                    PartialFilterExpression = Builders<ParkingSession>.Filter.Eq(
                        x => x.Status,
                        Parking.Domain.Enums.ParkingSessionStatus.Active)
                }),
            new CreateIndexModel<ParkingSession>(
                Builders<ParkingSession>.IndexKeys.Ascending(x => x.VehicleId).Ascending(x => x.CheckInTime),
                new CreateIndexOptions { Name = "ix_parking_sessions_vehicle_check_in" }),
            new CreateIndexModel<ParkingSession>(
                Builders<ParkingSession>.IndexKeys.Ascending(x => x.BuildingId).Ascending(x => x.Status),
                new CreateIndexOptions { Name = "ix_parking_sessions_building_status" }),
            new CreateIndexModel<ParkingSession>(
                Builders<ParkingSession>.IndexKeys.Ascending(x => x.ShiftId),
                new CreateIndexOptions { Name = "ix_parking_sessions_shift_id" })
        });
    }

    private async Task CreateOperationIndexesAsync()
    {
        await _context.ParkingSessionLogs.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<ParkingSessionLog>(
                Builders<ParkingSessionLog>.IndexKeys.Ascending(x => x.ParkingSessionId).Descending(x => x.CreatedAt),
                new CreateIndexOptions { Name = "ix_parking_session_logs_session_created_at" })
        });

        await _context.Shifts.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<Shift>(
                Builders<Shift>.IndexKeys.Ascending(x => x.StaffUserId).Ascending(x => x.Status),
                new CreateIndexOptions { Name = "ix_shifts_staff_status" }),
            new CreateIndexModel<Shift>(
                Builders<Shift>.IndexKeys.Ascending(x => x.BuildingId).Descending(x => x.OpenedAt),
                new CreateIndexOptions { Name = "ix_shifts_building_opened_at" })
        });

        await _context.IncidentReports.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<IncidentReport>(
                Builders<IncidentReport>.IndexKeys.Ascending(x => x.BuildingId).Ascending(x => x.Status),
                new CreateIndexOptions { Name = "ix_incident_reports_building_status" }),
            new CreateIndexModel<IncidentReport>(
                Builders<IncidentReport>.IndexKeys.Ascending(x => x.VehicleId),
                new CreateIndexOptions { Name = "ix_incident_reports_vehicle_id" })
        });

        await _context.Reservations.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<Reservation>(
                Builders<Reservation>.IndexKeys.Ascending(x => x.BuildingId).Ascending(x => x.Status).Ascending(x => x.ReservedFrom),
                new CreateIndexOptions { Name = "ix_reservations_building_status_from" }),
            new CreateIndexModel<Reservation>(
                Builders<Reservation>.IndexKeys.Ascending(x => x.VehicleId).Descending(x => x.ReservedFrom),
                new CreateIndexOptions { Name = "ix_reservations_vehicle_from" })
        });

        await _context.Feedbacks.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<Feedback>(
                Builders<Feedback>.IndexKeys.Ascending(x => x.Status).Descending(x => x.CreatedAt),
                new CreateIndexOptions { Name = "ix_feedbacks_status_created_at" }),
            new CreateIndexModel<Feedback>(
                Builders<Feedback>.IndexKeys.Ascending(x => x.VehicleId),
                new CreateIndexOptions { Name = "ix_feedbacks_vehicle_id" })
        });
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

        await _context.Notifications.Indexes.CreateOneAsync(new CreateIndexModel<Notification>(
            Builders<Notification>.IndexKeys.Ascending(x => x.UserId).Ascending(x => x.IsRead),
            new CreateIndexOptions { Name = "ix_notifications_user_unread" }));
    }
}
