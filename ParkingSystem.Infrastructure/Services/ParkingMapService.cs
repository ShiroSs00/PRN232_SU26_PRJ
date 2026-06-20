using MongoDB.Driver;
using ParkingSystem.Application.DTOs;
using ParkingSystem.Application.Services;
using ParkingSystem.Domain.Constants;
using ParkingSystem.Domain.Entities;
using ParkingSystem.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ParkingSystem.Infrastructure.Services;

public class ParkingMapService : IParkingMapService
{
    private readonly MongoDbContext _context;

    public ParkingMapService(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<FloorMapDto?> GetFloorMapAsync(string floorId)
    {
        var floor = await _context.Floors.Find(f => f.Id == floorId && f.IsActive).FirstOrDefaultAsync();
        if (floor == null)
        {
            return null;
        }

        var slots = await _context.ParkingSlots.Find(s => s.FloorId == floorId && s.IsActive).ToListAsync();

        var slotIds = slots.Select(s => s.Id).ToList();
        var activeSessions = await _context.ParkingSessions
            .Find(s => slotIds.Contains(s.ParkingSlotId) && 
                      (s.Status == ParkingSessionStatuses.Active || s.Status == ParkingSessionStatuses.LostTicket))
            .ToListAsync();

        var sessionMap = activeSessions.ToDictionary(s => s.ParkingSlotId);

        var mapSlots = slots.Select(s =>
        {
            var session = sessionMap.GetValueOrDefault(s.Id);
            OccupyingVehicleDto? vehicleDto = null;
            if (session != null)
            {
                vehicleDto = new OccupyingVehicleDto
                {
                    SessionId = session.Id,
                    PlateNumber = session.PlateNumber,
                    CheckInTime = session.CheckInTime,
                    IsMonthly = false
                };
            }

            return new MapSlotDto
            {
                SlotId = s.Id,
                Code = s.Code,
                Label = s.Label ?? s.Code,
                Row = s.Row,
                Column = s.Column,
                RowSpan = s.RowSpan > 0 ? s.RowSpan : 1,
                ColSpan = s.ColSpan > 0 ? s.ColSpan : 1,
                ZoneId = s.ZoneId,
                VehicleTypeId = s.VehicleTypeId,
                Status = s.Status,
                Vehicle = vehicleDto
            };
        }).ToList();

        var summary = new MapSummaryDto
        {
            TotalSlots = slots.Count,
            AvailableSlots = slots.Count(s => s.Status == ParkingSlotStatuses.Available),
            OccupiedSlots = slots.Count(s => s.Status == ParkingSlotStatuses.Occupied),
            ReservedSlots = slots.Count(s => s.Status == ParkingSlotStatuses.Reserved),
            MaintenanceSlots = slots.Count(s => s.Status == ParkingSlotStatuses.Maintenance) + slots.Count(s => s.Status == ParkingSlotStatuses.Locked)
        };

        return new FloorMapDto
        {
            FloorId = floor.Id,
            BuildingId = floor.BuildingId,
            FloorName = floor.Name,
            GridRows = floor.GridRows,
            GridCols = floor.GridCols,
            Slots = mapSlots,
            Summary = summary
        };
    }

    public async Task<IEnumerable<FloorDto>> GetFloorsByBuildingAsync(string buildingId)
    {
        var floors = await _context.Floors
            .Find(f => f.BuildingId == buildingId && f.IsActive)
            .SortBy(f => f.FloorNumber)
            .ToListAsync();

        return floors.Select(f => new FloorDto
        {
            Id = f.Id,
            BuildingId = f.BuildingId,
            FloorNumber = f.FloorNumber,
            Name = f.Name,
            GridRows = f.GridRows,
            GridCols = f.GridCols,
            IsActive = f.IsActive
        });
    }

    public async Task<bool> UpdateSlotPositionAsync(string slotId, UpdateSlotPositionDto dto)
    {
        var slot = await _context.ParkingSlots.Find(s => s.Id == slotId && s.IsActive).FirstOrDefaultAsync();
        if (slot == null)
        {
            return false;
        }

        slot.Row = dto.Row;
        slot.Column = dto.Column;
        slot.RowSpan = dto.RowSpan;
        slot.ColSpan = dto.ColSpan;
        slot.Label = dto.Label;
        slot.UpdatedAt = DateTime.UtcNow;

        var result = await _context.ParkingSlots.ReplaceOneAsync(s => s.Id == slotId, slot);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> GenerateGridLayoutAsync(string floorId, GenerateGridLayoutDto dto)
    {
        var floor = await _context.Floors.Find(f => f.Id == floorId && f.IsActive).FirstOrDefaultAsync();
        if (floor == null)
        {
            return false;
        }

        // Verify zone and vehicle type exist
        var zone = await _context.Zones.Find(z => z.Id == dto.ZoneId && z.IsActive).FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Zone with ID '{dto.ZoneId}' was not found.");
        var vehicleType = await _context.VehicleTypes.Find(vt => vt.Id == dto.VehicleTypeId && vt.IsActive).FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Vehicle type with ID '{dto.VehicleTypeId}' was not found.");

        // 1. Update Floor dimensions
        floor.GridRows = dto.GridRows;
        floor.GridCols = dto.GridCols;
        floor.UpdatedAt = DateTime.UtcNow;
        await _context.Floors.ReplaceOneAsync(f => f.Id == floorId, floor);

        // 2. Soft-delete existing slots of this floor
        var update = Builders<ParkingSlot>.Update
            .Set(s => s.IsActive, false)
            .Set(s => s.UpdatedAt, DateTime.UtcNow);
        await _context.ParkingSlots.UpdateManyAsync(s => s.FloorId == floorId && s.IsActive, update);

        // 3. Generate new grid slots
        var now = DateTime.UtcNow;
        var slots = new List<ParkingSlot>();
        int count = 1;

        for (int r = 1; r <= dto.GridRows; r++)
        {
            for (int c = 1; c <= dto.GridCols; c++)
            {
                var slotCode = $"{dto.Prefix}-R{r}C{c}";
                slots.Add(new ParkingSlot
                {
                    Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                    BuildingId = floor.BuildingId,
                    FloorId = floorId,
                    ZoneId = dto.ZoneId,
                    VehicleTypeId = dto.VehicleTypeId,
                    Code = slotCode,
                    Label = slotCode,
                    Status = ParkingSlotStatuses.Available,
                    Row = r,
                    Column = c,
                    RowSpan = 1,
                    ColSpan = 1,
                    IsActive = true,
                    CreatedAt = now
                });
                count++;
            }
        }

        await _context.ParkingSlots.InsertManyAsync(slots);
        return true;
    }
}
