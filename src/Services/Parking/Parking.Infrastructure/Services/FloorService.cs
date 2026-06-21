using MongoDB.Bson;
using MongoDB.Driver;
using Parking.Application.Abstractions;
using Parking.Application.Common;
using Parking.Application.DTOs;
using Parking.Application.DTOs.MasterData;
using Parking.Domain.Entities;
using Parking.Infrastructure.Persistence;

namespace Parking.Infrastructure.Services;

public class FloorService : IFloorService
{
    private readonly MongoDbContext _db;

    public FloorService(MongoDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PagedResult<FloorDto>>> GetListAsync(
        string? buildingId,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 200) pageSize = 200;

        var fb = Builders<Floor>.Filter;
        var filters = new List<FilterDefinition<Floor>>();
        if (!string.IsNullOrWhiteSpace(buildingId)) filters.Add(fb.Eq(x => x.BuildingId, buildingId));
        if (isActive.HasValue) filters.Add(fb.Eq(x => x.IsActive, isActive.Value));

        var filter = filters.Count == 0 ? fb.Empty : fb.And(filters);
        var total = await _db.Floors.CountDocumentsAsync(filter, cancellationToken: ct);

        var items = await _db.Floors.Find(filter)
            .SortBy(x => x.FloorNumber)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(ct);

        return Result<PagedResult<FloorDto>>.Ok(new PagedResult<FloorDto>
        {
            Items = items.Select(Map).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        });
    }

    public async Task<Result<FloorDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var entity = await _db.Floors.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (entity is null)
            return Result<FloorDto>.Fail("Floor not found.", ParkingErrorCodes.FloorNotFound);
        return Result<FloorDto>.Ok(Map(entity));
    }

    public async Task<Result<FloorDto>> CreateAsync(CreateFloorRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.BuildingId))
            return Result<FloorDto>.Fail("BuildingId is required.", ParkingErrorCodes.ValidationFailed);
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result<FloorDto>.Fail("Name is required.", ParkingErrorCodes.ValidationFailed);
        if (request.GridRows < 0 || request.GridCols < 0)
            return Result<FloorDto>.Fail("Grid dimensions must be non-negative.", ParkingErrorCodes.ValidationFailed);

        var building = await _db.Buildings.Find(x => x.Id == request.BuildingId).FirstOrDefaultAsync(ct);
        if (building is null)
            return Result<FloorDto>.Fail("Building not found.", ParkingErrorCodes.BuildingNotFound);

        var now = DateTime.UtcNow;
        var entity = new Floor
        {
            Id = ObjectId.GenerateNewId().ToString(),
            BuildingId = request.BuildingId.Trim(),
            FloorNumber = request.FloorNumber,
            Name = request.Name.Trim(),
            GridRows = request.GridRows,
            GridCols = request.GridCols,
            CreatedAt = now,
            IsActive = true
        };

        await _db.Floors.InsertOneAsync(entity, cancellationToken: ct);
        return Result<FloorDto>.Ok(Map(entity));
    }

    public async Task<Result<FloorDto>> UpdateAsync(string id, UpdateFloorRequest request, CancellationToken ct = default)
    {
        var entity = await _db.Floors.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (entity is null)
            return Result<FloorDto>.Fail("Floor not found.", ParkingErrorCodes.FloorNotFound);

        if (string.IsNullOrWhiteSpace(request.Name))
            return Result<FloorDto>.Fail("Name is required.", ParkingErrorCodes.ValidationFailed);
        if (request.GridRows < 0 || request.GridCols < 0)
            return Result<FloorDto>.Fail("Grid dimensions must be non-negative.", ParkingErrorCodes.ValidationFailed);

        var update = Builders<Floor>.Update
            .Set(x => x.FloorNumber, request.FloorNumber)
            .Set(x => x.Name, request.Name.Trim())
            .Set(x => x.GridRows, request.GridRows)
            .Set(x => x.GridCols, request.GridCols)
            .Set(x => x.IsActive, request.IsActive)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        await _db.Floors.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);
        var updated = await _db.Floors.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        return Result<FloorDto>.Ok(Map(updated!));
    }

    public async Task<Result> DeleteAsync(string id, CancellationToken ct = default)
    {
        var entity = await _db.Floors.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (entity is null)
            return Result.Fail("Floor not found.", ParkingErrorCodes.FloorNotFound);

        var update = Builders<Floor>.Update
            .Set(x => x.IsActive, false)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);
        await _db.Floors.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);
        return Result.Ok();
    }

    private static FloorDto Map(Floor x) => new()
    {
        Id = x.Id,
        BuildingId = x.BuildingId,
        FloorNumber = x.FloorNumber,
        Name = x.Name,
        GridRows = x.GridRows,
        GridCols = x.GridCols,
        IsActive = x.IsActive,
        CreatedAt = x.CreatedAt,
        UpdatedAt = x.UpdatedAt
    };
}
