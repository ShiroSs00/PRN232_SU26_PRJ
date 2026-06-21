using MongoDB.Bson;
using MongoDB.Driver;
using Parking.Application.Abstractions;
using Parking.Application.Common;
using Parking.Application.DTOs;
using Parking.Application.DTOs.MasterData;
using Parking.Domain.Entities;
using Parking.Infrastructure.Persistence;

namespace Parking.Infrastructure.Services;

public class ZoneService : IZoneService
{
    private readonly MongoDbContext _db;

    public ZoneService(MongoDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PagedResult<ZoneDto>>> GetListAsync(
        string? buildingId,
        string? floorId,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 200) pageSize = 200;

        var fb = Builders<Zone>.Filter;
        var filters = new List<FilterDefinition<Zone>>();
        if (!string.IsNullOrWhiteSpace(buildingId)) filters.Add(fb.Eq(x => x.BuildingId, buildingId));
        if (!string.IsNullOrWhiteSpace(floorId)) filters.Add(fb.Eq(x => x.FloorId, floorId));
        if (isActive.HasValue) filters.Add(fb.Eq(x => x.IsActive, isActive.Value));

        var filter = filters.Count == 0 ? fb.Empty : fb.And(filters);
        var total = await _db.Zones.CountDocumentsAsync(filter, cancellationToken: ct);

        var items = await _db.Zones.Find(filter)
            .SortByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(ct);

        return Result<PagedResult<ZoneDto>>.Ok(new PagedResult<ZoneDto>
        {
            Items = items.Select(Map).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        });
    }

    public async Task<Result<ZoneDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var entity = await _db.Zones.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (entity is null)
            return Result<ZoneDto>.Fail("Zone not found.", ParkingErrorCodes.ZoneNotFound);
        return Result<ZoneDto>.Ok(Map(entity));
    }

    public async Task<Result<ZoneDto>> CreateAsync(CreateZoneRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.BuildingId))
            return Result<ZoneDto>.Fail("BuildingId is required.", ParkingErrorCodes.ValidationFailed);
        if (string.IsNullOrWhiteSpace(request.FloorId))
            return Result<ZoneDto>.Fail("FloorId is required.", ParkingErrorCodes.ValidationFailed);
        if (string.IsNullOrWhiteSpace(request.VehicleTypeId))
            return Result<ZoneDto>.Fail("VehicleTypeId is required.", ParkingErrorCodes.ValidationFailed);
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result<ZoneDto>.Fail("Name is required.", ParkingErrorCodes.ValidationFailed);
        if (request.Capacity < 0)
            return Result<ZoneDto>.Fail("Capacity must be non-negative.", ParkingErrorCodes.ValidationFailed);

        var building = await _db.Buildings.Find(x => x.Id == request.BuildingId).FirstOrDefaultAsync(ct);
        if (building is null)
            return Result<ZoneDto>.Fail("Building not found.", ParkingErrorCodes.BuildingNotFound);

        var floor = await _db.Floors.Find(x => x.Id == request.FloorId).FirstOrDefaultAsync(ct);
        if (floor is null)
            return Result<ZoneDto>.Fail("Floor not found.", ParkingErrorCodes.FloorNotFound);

        var vehicleType = await _db.VehicleTypes.Find(x => x.Id == request.VehicleTypeId).FirstOrDefaultAsync(ct);
        if (vehicleType is null)
            return Result<ZoneDto>.Fail("Vehicle type not found.", ParkingErrorCodes.VehicleTypeNotFound);

        var now = DateTime.UtcNow;
        var entity = new Zone
        {
            Id = ObjectId.GenerateNewId().ToString(),
            BuildingId = request.BuildingId.Trim(),
            FloorId = request.FloorId.Trim(),
            VehicleTypeId = request.VehicleTypeId.Trim(),
            Name = request.Name.Trim(),
            Capacity = request.Capacity,
            CurrentOccupancy = 0,
            CreatedAt = now,
            IsActive = true
        };

        await _db.Zones.InsertOneAsync(entity, cancellationToken: ct);
        return Result<ZoneDto>.Ok(Map(entity));
    }

    public async Task<Result<ZoneDto>> UpdateAsync(string id, UpdateZoneRequest request, CancellationToken ct = default)
    {
        var entity = await _db.Zones.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (entity is null)
            return Result<ZoneDto>.Fail("Zone not found.", ParkingErrorCodes.ZoneNotFound);

        if (string.IsNullOrWhiteSpace(request.VehicleTypeId))
            return Result<ZoneDto>.Fail("VehicleTypeId is required.", ParkingErrorCodes.ValidationFailed);
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result<ZoneDto>.Fail("Name is required.", ParkingErrorCodes.ValidationFailed);
        if (request.Capacity < 0)
            return Result<ZoneDto>.Fail("Capacity must be non-negative.", ParkingErrorCodes.ValidationFailed);

        var vehicleType = await _db.VehicleTypes.Find(x => x.Id == request.VehicleTypeId).FirstOrDefaultAsync(ct);
        if (vehicleType is null)
            return Result<ZoneDto>.Fail("Vehicle type not found.", ParkingErrorCodes.VehicleTypeNotFound);

        var update = Builders<Zone>.Update
            .Set(x => x.VehicleTypeId, request.VehicleTypeId.Trim())
            .Set(x => x.Name, request.Name.Trim())
            .Set(x => x.Capacity, request.Capacity)
            .Set(x => x.IsActive, request.IsActive)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        await _db.Zones.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);
        var updated = await _db.Zones.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        return Result<ZoneDto>.Ok(Map(updated!));
    }

    public async Task<Result> DeleteAsync(string id, CancellationToken ct = default)
    {
        var entity = await _db.Zones.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (entity is null)
            return Result.Fail("Zone not found.", ParkingErrorCodes.ZoneNotFound);

        var update = Builders<Zone>.Update
            .Set(x => x.IsActive, false)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);
        await _db.Zones.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);
        return Result.Ok();
    }

    private static ZoneDto Map(Zone x) => new()
    {
        Id = x.Id,
        BuildingId = x.BuildingId,
        FloorId = x.FloorId,
        VehicleTypeId = x.VehicleTypeId,
        Name = x.Name,
        Capacity = x.Capacity,
        CurrentOccupancy = x.CurrentOccupancy,
        IsActive = x.IsActive,
        CreatedAt = x.CreatedAt,
        UpdatedAt = x.UpdatedAt
    };
}
