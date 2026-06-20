using MongoDB.Driver;
using ParkingSystem.Application.DTOs;
using ParkingSystem.Application.Services;
using ParkingSystem.Domain.Entities;
using ParkingSystem.Infrastructure.Persistence;

namespace ParkingSystem.Infrastructure.Services;

public class ParkingSlotService : IParkingSlotService
{
    private readonly MongoDbContext _context;

    public ParkingSlotService(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ParkingSlotDto>> GetAllAsync(
        string? buildingId = null,
        string? floorId = null,
        string? zoneId = null,
        string? vehicleTypeId = null,
        string? status = null)
    {
        var filterBuilder = Builders<ParkingSlot>.Filter;
        var filter = filterBuilder.Empty;

        if (!string.IsNullOrEmpty(buildingId))
        {
            filter &= filterBuilder.Eq(s => s.BuildingId, buildingId);
        }
        if (!string.IsNullOrEmpty(floorId))
        {
            filter &= filterBuilder.Eq(s => s.FloorId, floorId);
        }
        if (!string.IsNullOrEmpty(zoneId))
        {
            filter &= filterBuilder.Eq(s => s.ZoneId, zoneId);
        }
        if (!string.IsNullOrEmpty(vehicleTypeId))
        {
            filter &= filterBuilder.Eq(s => s.VehicleTypeId, vehicleTypeId);
        }
        if (!string.IsNullOrEmpty(status))
        {
            filter &= filterBuilder.Eq(s => s.Status, status);
        }

        filter &= filterBuilder.Eq(s => s.IsActive, true);

        var slots = await _context.ParkingSlots.Find(filter).ToListAsync();

        if (!slots.Any())
        {
            return Enumerable.Empty<ParkingSlotDto>();
        }

        var buildingIds = slots.Select(s => s.BuildingId).Distinct().ToList();
        var floorIds = slots.Select(s => s.FloorId).Distinct().ToList();
        var zoneIds = slots.Select(s => s.ZoneId).Distinct().ToList();
        var vehicleTypeIds = slots.Select(s => s.VehicleTypeId).Distinct().ToList();

        var buildings = await _context.Buildings.Find(b => buildingIds.Contains(b.Id)).ToListAsync();
        var floors = await _context.Floors.Find(f => floorIds.Contains(f.Id)).ToListAsync();
        var zones = await _context.Zones.Find(z => zoneIds.Contains(z.Id)).ToListAsync();
        var vehicleTypes = await _context.VehicleTypes.Find(vt => vehicleTypeIds.Contains(vt.Id)).ToListAsync();

        var buildingMap = buildings.ToDictionary(b => b.Id);
        var floorMap = floors.ToDictionary(f => f.Id);
        var zoneMap = zones.ToDictionary(z => z.Id);
        var vehicleTypeMap = vehicleTypes.ToDictionary(vt => vt.Id);

        return slots.Select(s => MapToDto(s, buildingMap, floorMap, zoneMap, vehicleTypeMap));
    }

    public async Task<ParkingSlotDto?> GetByIdAsync(string id)
    {
        var slot = await _context.ParkingSlots
            .Find(s => s.Id == id && s.IsActive)
            .FirstOrDefaultAsync();

        if (slot == null)
        {
            return null;
        }

        var building = await _context.Buildings.Find(b => b.Id == slot.BuildingId).FirstOrDefaultAsync();
        var floor = await _context.Floors.Find(f => f.Id == slot.FloorId).FirstOrDefaultAsync();
        var zone = await _context.Zones.Find(z => z.Id == slot.ZoneId).FirstOrDefaultAsync();
        var vehicleType = await _context.VehicleTypes.Find(vt => vt.Id == slot.VehicleTypeId).FirstOrDefaultAsync();

        return MapToDto(slot, building, floor, zone, vehicleType);
    }

    public async Task<IEnumerable<ParkingSlotDto>> GetByZoneIdAsync(string zoneId)
    {
        return await GetAllAsync(zoneId: zoneId);
    }

    public async Task<ParkingSlotDto> CreateAsync(CreateParkingSlotDto dto)
    {
        var building = await _context.Buildings.Find(b => b.Id == dto.BuildingId).FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Building with ID '{dto.BuildingId}' was not found.");

        var floor = await _context.Floors.Find(f => f.Id == dto.FloorId).FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Floor with ID '{dto.FloorId}' was not found.");

        var zone = await _context.Zones.Find(z => z.Id == dto.ZoneId).FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Zone with ID '{dto.ZoneId}' was not found.");

        var vehicleType = await _context.VehicleTypes.Find(vt => vt.Id == dto.VehicleTypeId).FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Vehicle type with ID '{dto.VehicleTypeId}' was not found.");

        var codeExists = await _context.ParkingSlots
            .Find(s => s.Code == dto.Code && s.IsActive)
            .AnyAsync();

        if (codeExists)
        {
            throw new InvalidOperationException($"Parking slot with code '{dto.Code}' already exists.");
        }

        var now = DateTime.UtcNow;
        var slot = new ParkingSlot
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            BuildingId = dto.BuildingId,
            FloorId = dto.FloorId,
            ZoneId = dto.ZoneId,
            VehicleTypeId = dto.VehicleTypeId,
            Code = dto.Code,
            Status = Domain.Constants.ParkingSlotStatuses.Available,
            Row = dto.Row,
            Column = dto.Column,
            PositionX = dto.PositionX,
            PositionY = dto.PositionY,
            Width = dto.Width,
            Height = dto.Height,
            IsActive = true,
            CreatedAt = now
        };

        await _context.ParkingSlots.InsertOneAsync(slot);

        return MapToDto(slot, building, floor, zone, vehicleType);
    }

    public async Task<ParkingSlotDto?> UpdateAsync(string id, UpdateParkingSlotDto dto)
    {
        var slot = await _context.ParkingSlots
            .Find(s => s.Id == id && s.IsActive)
            .FirstOrDefaultAsync();

        if (slot == null)
        {
            return null;
        }

        if (slot.Code != dto.Code)
        {
            var codeExists = await _context.ParkingSlots
                .Find(s => s.Code == dto.Code && s.IsActive)
                .AnyAsync();

            if (codeExists)
            {
                throw new InvalidOperationException($"Parking slot with code '{dto.Code}' already exists.");
            }
        }

        var vehicleType = await _context.VehicleTypes.Find(vt => vt.Id == dto.VehicleTypeId).FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Vehicle type with ID '{dto.VehicleTypeId}' was not found.");

        var building = await _context.Buildings.Find(b => b.Id == slot.BuildingId).FirstOrDefaultAsync();
        var floor = await _context.Floors.Find(f => f.Id == slot.FloorId).FirstOrDefaultAsync();
        var zone = await _context.Zones.Find(z => z.Id == slot.ZoneId).FirstOrDefaultAsync();

        slot.Code = dto.Code;
        slot.VehicleTypeId = dto.VehicleTypeId;
        slot.Row = dto.Row;
        slot.Column = dto.Column;
        slot.PositionX = dto.PositionX;
        slot.PositionY = dto.PositionY;
        slot.Width = dto.Width;
        slot.Height = dto.Height;
        slot.UpdatedAt = DateTime.UtcNow;

        await _context.ParkingSlots.ReplaceOneAsync(s => s.Id == id, slot);

        return MapToDto(slot, building, floor, zone, vehicleType);
    }

    public async Task<ParkingSlotDto?> UpdateStatusAsync(string id, string status)
    {
        var slot = await _context.ParkingSlots
            .Find(s => s.Id == id && s.IsActive)
            .FirstOrDefaultAsync();

        if (slot == null)
        {
            return null;
        }

        var validStatuses = new[]
        {
            Domain.Constants.ParkingSlotStatuses.Available,
            Domain.Constants.ParkingSlotStatuses.Occupied,
            Domain.Constants.ParkingSlotStatuses.Reserved,
            Domain.Constants.ParkingSlotStatuses.Maintenance,
            Domain.Constants.ParkingSlotStatuses.Locked
        };

        if (!validStatuses.Contains(status))
        {
            throw new ArgumentException($"Invalid status '{status}'.");
        }

        slot.Status = status;
        slot.UpdatedAt = DateTime.UtcNow;

        await _context.ParkingSlots.ReplaceOneAsync(s => s.Id == id, slot);

        var building = await _context.Buildings.Find(b => b.Id == slot.BuildingId).FirstOrDefaultAsync();
        var floor = await _context.Floors.Find(f => f.Id == slot.FloorId).FirstOrDefaultAsync();
        var zone = await _context.Zones.Find(z => z.Id == slot.ZoneId).FirstOrDefaultAsync();
        var vehicleType = await _context.VehicleTypes.Find(vt => vt.Id == slot.VehicleTypeId).FirstOrDefaultAsync();

        return MapToDto(slot, building, floor, zone, vehicleType);
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var update = Builders<ParkingSlot>.Update
            .Set(s => s.IsActive, false)
            .Set(s => s.UpdatedAt, DateTime.UtcNow);

        var result = await _context.ParkingSlots.UpdateOneAsync(s => s.Id == id && s.IsActive, update);

        return result.ModifiedCount > 0;
    }

    private static ParkingSlotDto MapToDto(
        ParkingSlot slot,
        Dictionary<string, Building> buildingMap,
        Dictionary<string, Floor> floorMap,
        Dictionary<string, Zone> zoneMap,
        Dictionary<string, VehicleType> vehicleTypeMap)
    {
        buildingMap.TryGetValue(slot.BuildingId, out var building);
        floorMap.TryGetValue(slot.FloorId, out var floor);
        zoneMap.TryGetValue(slot.ZoneId, out var zone);
        vehicleTypeMap.TryGetValue(slot.VehicleTypeId, out var vehicleType);

        return MapToDto(slot, building, floor, zone, vehicleType);
    }

    private static ParkingSlotDto MapToDto(
        ParkingSlot slot,
        Building? building,
        Floor? floor,
        Zone? zone,
        VehicleType? vehicleType)
    {
        return new ParkingSlotDto
        {
            Id = slot.Id,
            BuildingId = slot.BuildingId,
            FloorId = slot.FloorId,
            ZoneId = slot.ZoneId,
            VehicleTypeId = slot.VehicleTypeId,
            Code = slot.Code,
            Status = slot.Status,
            Row = slot.Row,
            Column = slot.Column,
            PositionX = slot.PositionX,
            PositionY = slot.PositionY,
            Width = slot.Width,
            Height = slot.Height,
            BuildingName = building?.Name,
            FloorName = floor?.Name,
            ZoneName = zone?.Name,
            VehicleTypeName = vehicleType?.Name,
            IsActive = slot.IsActive,
            CreatedAt = slot.CreatedAt,
            UpdatedAt = slot.UpdatedAt
        };
    }
}
