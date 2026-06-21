using MongoDB.Driver;
using Parking.Application.Abstractions;
using Parking.Application.Common;
using Parking.Application.DTOs.Map;
using Parking.Application.DTOs.Realtime;
using Parking.Domain.Entities;
using Parking.Domain.Enums;
using Parking.Infrastructure.Persistence;

namespace Parking.Infrastructure.Services;

public class ParkingMapService : IParkingMapService
{
    private readonly MongoDbContext _db;

    public ParkingMapService(MongoDbContext db)
    {
        _db = db;
    }

    public async Task<Result<FloorMapDto>> GetFloorMapAsync(string floorId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(floorId))
            return Result<FloorMapDto>.Fail("FloorId is required.", ParkingErrorCodes.ValidationFailed);

        var floor = await _db.Floors.Find(x => x.Id == floorId).FirstOrDefaultAsync(ct);
        if (floor is null)
            return Result<FloorMapDto>.Fail("Floor not found.", ParkingErrorCodes.FloorNotFound);

        var slots = await _db.ParkingSlots.Find(x => x.FloorId == floorId)
            .SortBy(x => x.Row).ThenBy(x => x.Column)
            .ToListAsync(ct);

        // Batch-load sessions for occupied slots to avoid N+1 queries.
        var sessionIds = slots
            .Where(s => s.Status == SlotStatus.Occupied && !string.IsNullOrWhiteSpace(s.CurrentSessionId))
            .Select(s => s.CurrentSessionId!)
            .Distinct()
            .ToList();

        var sessionMap = new Dictionary<string, ParkingSession>();
        if (sessionIds.Count > 0)
        {
            var sessions = await _db.ParkingSessions
                .Find(Builders<ParkingSession>.Filter.In(x => x.Id, sessionIds))
                .ToListAsync(ct);
            foreach (var s in sessions)
                sessionMap[s.Id] = s;
        }

        var mapSlots = slots.Select(s =>
        {
            OccupyingVehicleDto? vehicle = null;
            if (s.Status == SlotStatus.Occupied
                && !string.IsNullOrWhiteSpace(s.CurrentSessionId)
                && sessionMap.TryGetValue(s.CurrentSessionId!, out var session))
            {
                vehicle = new OccupyingVehicleDto
                {
                    SessionId = session.Id,
                    PlateNumber = session.PlateNumber,
                    VehicleTypeId = session.VehicleTypeId,
                    CheckInTime = session.CheckInTime,
                    IsMonthly = session.IsMonthly
                };
            }

            return new MapSlotDto
            {
                SlotId = s.Id,
                Code = s.Code,
                Label = s.Label,
                Row = s.Row,
                Column = s.Column,
                RowSpan = s.RowSpan,
                ColSpan = s.ColSpan,
                ZoneId = s.ZoneId,
                VehicleTypeId = s.VehicleTypeId,
                Status = s.Status,
                Vehicle = vehicle
            };
        }).ToList();

        var summary = new MapSummaryDto
        {
            Total = slots.Count,
            Available = slots.Count(x => x.Status == SlotStatus.Available),
            Occupied = slots.Count(x => x.Status == SlotStatus.Occupied),
            Reserved = slots.Count(x => x.Status == SlotStatus.Reserved),
            Maintenance = slots.Count(x => x.Status == SlotStatus.Maintenance)
        };

        return Result<FloorMapDto>.Ok(new FloorMapDto
        {
            FloorId = floor.Id,
            BuildingId = floor.BuildingId,
            FloorName = floor.Name,
            GridRows = floor.GridRows,
            GridCols = floor.GridCols,
            Slots = mapSlots,
            Summary = summary
        });
    }

    public async Task<Result<List<FloorOptionDto>>> GetFloorsByBuildingAsync(string buildingId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(buildingId))
            return Result<List<FloorOptionDto>>.Fail("BuildingId is required.", ParkingErrorCodes.ValidationFailed);

        var building = await _db.Buildings.Find(x => x.Id == buildingId).FirstOrDefaultAsync(ct);
        if (building is null)
            return Result<List<FloorOptionDto>>.Fail("Building not found.", ParkingErrorCodes.BuildingNotFound);

        var floors = await _db.Floors.Find(x => x.BuildingId == buildingId)
            .SortBy(x => x.FloorNumber)
            .ToListAsync(ct);

        var options = floors.Select(f => new FloorOptionDto
        {
            FloorId = f.Id,
            BuildingId = f.BuildingId,
            FloorNumber = f.FloorNumber,
            Name = f.Name,
            GridRows = f.GridRows,
            GridCols = f.GridCols
        }).ToList();

        return Result<List<FloorOptionDto>>.Ok(options);
    }
}
