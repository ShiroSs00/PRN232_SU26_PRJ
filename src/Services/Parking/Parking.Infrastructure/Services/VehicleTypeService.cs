using MongoDB.Bson;
using MongoDB.Driver;
using Parking.Application.Abstractions;
using Parking.Application.Common;
using Parking.Application.DTOs;
using Parking.Application.DTOs.MasterData;
using Parking.Domain.Entities;
using Parking.Infrastructure.Persistence;

namespace Parking.Infrastructure.Services;

public class VehicleTypeService : IVehicleTypeService
{
    private readonly MongoDbContext _db;

    public VehicleTypeService(MongoDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PagedResult<VehicleTypeDto>>> GetListAsync(
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 200) pageSize = 200;

        var fb = Builders<VehicleType>.Filter;
        var filters = new List<FilterDefinition<VehicleType>>();
        if (!string.IsNullOrWhiteSpace(search))
            filters.Add(fb.Regex(x => x.Name, new BsonRegularExpression(search.Trim(), "i")));
        if (isActive.HasValue) filters.Add(fb.Eq(x => x.IsActive, isActive.Value));

        var filter = filters.Count == 0 ? fb.Empty : fb.And(filters);
        var total = await _db.VehicleTypes.CountDocumentsAsync(filter, cancellationToken: ct);

        var items = await _db.VehicleTypes.Find(filter)
            .SortBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(ct);

        return Result<PagedResult<VehicleTypeDto>>.Ok(new PagedResult<VehicleTypeDto>
        {
            Items = items.Select(Map).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        });
    }

    public async Task<Result<VehicleTypeDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var entity = await _db.VehicleTypes.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (entity is null)
            return Result<VehicleTypeDto>.Fail("Vehicle type not found.", ParkingErrorCodes.VehicleTypeNotFound);
        return Result<VehicleTypeDto>.Ok(Map(entity));
    }

    public async Task<Result<VehicleTypeDto>> CreateAsync(CreateVehicleTypeRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result<VehicleTypeDto>.Fail("Name is required.", ParkingErrorCodes.ValidationFailed);

        var name = request.Name.Trim();
        var fb = Builders<VehicleType>.Filter;
        var duplicate = await _db.VehicleTypes
            .Find(fb.Regex(x => x.Name, new BsonRegularExpression($"^{System.Text.RegularExpressions.Regex.Escape(name)}$", "i")))
            .FirstOrDefaultAsync(ct);
        if (duplicate is not null)
            return Result<VehicleTypeDto>.Fail("A vehicle type with the same name already exists.", ParkingErrorCodes.DuplicateVehicleType);

        var now = DateTime.UtcNow;
        var entity = new VehicleType
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Name = name,
            Description = request.Description?.Trim(),
            CreatedAt = now,
            IsActive = true
        };

        await _db.VehicleTypes.InsertOneAsync(entity, cancellationToken: ct);
        return Result<VehicleTypeDto>.Ok(Map(entity));
    }

    public async Task<Result<VehicleTypeDto>> UpdateAsync(string id, UpdateVehicleTypeRequest request, CancellationToken ct = default)
    {
        var entity = await _db.VehicleTypes.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (entity is null)
            return Result<VehicleTypeDto>.Fail("Vehicle type not found.", ParkingErrorCodes.VehicleTypeNotFound);

        if (string.IsNullOrWhiteSpace(request.Name))
            return Result<VehicleTypeDto>.Fail("Name is required.", ParkingErrorCodes.ValidationFailed);

        var name = request.Name.Trim();
        var fb = Builders<VehicleType>.Filter;
        var duplicate = await _db.VehicleTypes
            .Find(fb.And(
                fb.Regex(x => x.Name, new BsonRegularExpression($"^{System.Text.RegularExpressions.Regex.Escape(name)}$", "i")),
                fb.Ne(x => x.Id, id)))
            .FirstOrDefaultAsync(ct);
        if (duplicate is not null)
            return Result<VehicleTypeDto>.Fail("A vehicle type with the same name already exists.", ParkingErrorCodes.DuplicateVehicleType);

        var update = Builders<VehicleType>.Update
            .Set(x => x.Name, name)
            .Set(x => x.Description, request.Description?.Trim())
            .Set(x => x.IsActive, request.IsActive)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        await _db.VehicleTypes.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);
        var updated = await _db.VehicleTypes.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        return Result<VehicleTypeDto>.Ok(Map(updated!));
    }

    public async Task<Result> DeleteAsync(string id, CancellationToken ct = default)
    {
        var entity = await _db.VehicleTypes.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (entity is null)
            return Result.Fail("Vehicle type not found.", ParkingErrorCodes.VehicleTypeNotFound);

        var update = Builders<VehicleType>.Update
            .Set(x => x.IsActive, false)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);
        await _db.VehicleTypes.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);
        return Result.Ok();
    }

    private static VehicleTypeDto Map(VehicleType x) => new()
    {
        Id = x.Id,
        Name = x.Name,
        Description = x.Description,
        IsActive = x.IsActive,
        CreatedAt = x.CreatedAt,
        UpdatedAt = x.UpdatedAt
    };
}
