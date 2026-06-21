using MongoDB.Bson;
using MongoDB.Driver;
using Parking.Application.Abstractions;
using Parking.Application.Common;
using Parking.Application.DTOs;
using Parking.Application.DTOs.MasterData;
using Parking.Domain.Entities;
using Parking.Infrastructure.Persistence;

namespace Parking.Infrastructure.Services;

public class BuildingService : IBuildingService
{
    private readonly MongoDbContext _db;

    public BuildingService(MongoDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PagedResult<BuildingDto>>> GetListAsync(
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 200) pageSize = 200;

        var fb = Builders<Building>.Filter;
        var filters = new List<FilterDefinition<Building>>();
        if (!string.IsNullOrWhiteSpace(search))
            filters.Add(fb.Regex(x => x.Name, new BsonRegularExpression(search.Trim(), "i")));
        if (isActive.HasValue) filters.Add(fb.Eq(x => x.IsActive, isActive.Value));

        var filter = filters.Count == 0 ? fb.Empty : fb.And(filters);
        var total = await _db.Buildings.CountDocumentsAsync(filter, cancellationToken: ct);

        var items = await _db.Buildings.Find(filter)
            .SortByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(ct);

        return Result<PagedResult<BuildingDto>>.Ok(new PagedResult<BuildingDto>
        {
            Items = items.Select(Map).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        });
    }

    public async Task<Result<BuildingDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var entity = await _db.Buildings.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (entity is null)
            return Result<BuildingDto>.Fail("Building not found.", ParkingErrorCodes.BuildingNotFound);
        return Result<BuildingDto>.Ok(Map(entity));
    }

    public async Task<Result<BuildingDto>> CreateAsync(CreateBuildingRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result<BuildingDto>.Fail("Name is required.", ParkingErrorCodes.ValidationFailed);
        if (string.IsNullOrWhiteSpace(request.Address))
            return Result<BuildingDto>.Fail("Address is required.", ParkingErrorCodes.ValidationFailed);

        var now = DateTime.UtcNow;
        var entity = new Building
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Name = request.Name.Trim(),
            Address = request.Address.Trim(),
            Description = request.Description?.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            OpeningTime = request.OpeningTime,
            ClosingTime = request.ClosingTime,
            CreatedAt = now,
            IsActive = true
        };

        await _db.Buildings.InsertOneAsync(entity, cancellationToken: ct);
        return Result<BuildingDto>.Ok(Map(entity));
    }

    public async Task<Result<BuildingDto>> UpdateAsync(string id, UpdateBuildingRequest request, CancellationToken ct = default)
    {
        var entity = await _db.Buildings.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (entity is null)
            return Result<BuildingDto>.Fail("Building not found.", ParkingErrorCodes.BuildingNotFound);

        if (string.IsNullOrWhiteSpace(request.Name))
            return Result<BuildingDto>.Fail("Name is required.", ParkingErrorCodes.ValidationFailed);
        if (string.IsNullOrWhiteSpace(request.Address))
            return Result<BuildingDto>.Fail("Address is required.", ParkingErrorCodes.ValidationFailed);

        var update = Builders<Building>.Update
            .Set(x => x.Name, request.Name.Trim())
            .Set(x => x.Address, request.Address.Trim())
            .Set(x => x.Description, request.Description?.Trim())
            .Set(x => x.PhoneNumber, request.PhoneNumber?.Trim())
            .Set(x => x.OpeningTime, request.OpeningTime)
            .Set(x => x.ClosingTime, request.ClosingTime)
            .Set(x => x.IsActive, request.IsActive)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        await _db.Buildings.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);
        var updated = await _db.Buildings.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        return Result<BuildingDto>.Ok(Map(updated!));
    }

    public async Task<Result> DeleteAsync(string id, CancellationToken ct = default)
    {
        var entity = await _db.Buildings.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (entity is null)
            return Result.Fail("Building not found.", ParkingErrorCodes.BuildingNotFound);

        var update = Builders<Building>.Update
            .Set(x => x.IsActive, false)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);
        await _db.Buildings.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);
        return Result.Ok();
    }

    private static BuildingDto Map(Building x) => new()
    {
        Id = x.Id,
        Name = x.Name,
        Address = x.Address,
        Description = x.Description,
        PhoneNumber = x.PhoneNumber,
        OpeningTime = x.OpeningTime,
        ClosingTime = x.ClosingTime,
        IsActive = x.IsActive,
        CreatedAt = x.CreatedAt,
        UpdatedAt = x.UpdatedAt
    };
}
