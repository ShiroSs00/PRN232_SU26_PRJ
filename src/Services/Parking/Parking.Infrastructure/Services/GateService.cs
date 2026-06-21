using MongoDB.Bson;
using MongoDB.Driver;
using Parking.Application.Abstractions;
using Parking.Application.Common;
using Parking.Application.DTOs;
using Parking.Application.DTOs.MasterData;
using Parking.Domain.Entities;
using Parking.Infrastructure.Persistence;

namespace Parking.Infrastructure.Services;

public class GateService : IGateService
{
    private readonly MongoDbContext _db;

    public GateService(MongoDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PagedResult<GateDto>>> GetListAsync(
        string? buildingId,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 200) pageSize = 200;

        var fb = Builders<Gate>.Filter;
        var filters = new List<FilterDefinition<Gate>>();
        if (!string.IsNullOrWhiteSpace(buildingId)) filters.Add(fb.Eq(x => x.BuildingId, buildingId));
        if (isActive.HasValue) filters.Add(fb.Eq(x => x.IsActive, isActive.Value));

        var filter = filters.Count == 0 ? fb.Empty : fb.And(filters);
        var total = await _db.Gates.CountDocumentsAsync(filter, cancellationToken: ct);

        var items = await _db.Gates.Find(filter)
            .SortBy(x => x.Code)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(ct);

        return Result<PagedResult<GateDto>>.Ok(new PagedResult<GateDto>
        {
            Items = items.Select(Map).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        });
    }

    public async Task<Result<GateDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var entity = await _db.Gates.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (entity is null)
            return Result<GateDto>.Fail("Gate not found.", ParkingErrorCodes.GateNotFound);
        return Result<GateDto>.Ok(Map(entity));
    }

    public async Task<Result<GateDto>> CreateAsync(CreateGateRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.BuildingId))
            return Result<GateDto>.Fail("BuildingId is required.", ParkingErrorCodes.ValidationFailed);
        if (string.IsNullOrWhiteSpace(request.Code))
            return Result<GateDto>.Fail("Code is required.", ParkingErrorCodes.ValidationFailed);
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result<GateDto>.Fail("Name is required.", ParkingErrorCodes.ValidationFailed);

        var building = await _db.Buildings.Find(x => x.Id == request.BuildingId).FirstOrDefaultAsync(ct);
        if (building is null)
            return Result<GateDto>.Fail("Building not found.", ParkingErrorCodes.BuildingNotFound);

        var code = request.Code.Trim();
        var fb = Builders<Gate>.Filter;
        var duplicate = await _db.Gates
            .Find(fb.And(
                fb.Eq(x => x.BuildingId, request.BuildingId.Trim()),
                fb.Regex(x => x.Code, new BsonRegularExpression($"^{System.Text.RegularExpressions.Regex.Escape(code)}$", "i"))))
            .FirstOrDefaultAsync(ct);
        if (duplicate is not null)
            return Result<GateDto>.Fail("A gate with the same code already exists in this building.", ParkingErrorCodes.DuplicateGateCode);

        var now = DateTime.UtcNow;
        var entity = new Gate
        {
            Id = ObjectId.GenerateNewId().ToString(),
            BuildingId = request.BuildingId.Trim(),
            Code = code,
            Name = request.Name.Trim(),
            Type = request.Type,
            CreatedAt = now,
            IsActive = true
        };

        await _db.Gates.InsertOneAsync(entity, cancellationToken: ct);
        return Result<GateDto>.Ok(Map(entity));
    }

    public async Task<Result<GateDto>> UpdateAsync(string id, UpdateGateRequest request, CancellationToken ct = default)
    {
        var entity = await _db.Gates.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (entity is null)
            return Result<GateDto>.Fail("Gate not found.", ParkingErrorCodes.GateNotFound);

        if (string.IsNullOrWhiteSpace(request.Code))
            return Result<GateDto>.Fail("Code is required.", ParkingErrorCodes.ValidationFailed);
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result<GateDto>.Fail("Name is required.", ParkingErrorCodes.ValidationFailed);

        var code = request.Code.Trim();
        var fb = Builders<Gate>.Filter;
        var duplicate = await _db.Gates
            .Find(fb.And(
                fb.Eq(x => x.BuildingId, entity.BuildingId),
                fb.Regex(x => x.Code, new BsonRegularExpression($"^{System.Text.RegularExpressions.Regex.Escape(code)}$", "i")),
                fb.Ne(x => x.Id, id)))
            .FirstOrDefaultAsync(ct);
        if (duplicate is not null)
            return Result<GateDto>.Fail("A gate with the same code already exists in this building.", ParkingErrorCodes.DuplicateGateCode);

        var update = Builders<Gate>.Update
            .Set(x => x.Code, code)
            .Set(x => x.Name, request.Name.Trim())
            .Set(x => x.Type, request.Type)
            .Set(x => x.IsActive, request.IsActive)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        await _db.Gates.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);
        var updated = await _db.Gates.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        return Result<GateDto>.Ok(Map(updated!));
    }

    public async Task<Result> DeleteAsync(string id, CancellationToken ct = default)
    {
        var entity = await _db.Gates.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (entity is null)
            return Result.Fail("Gate not found.", ParkingErrorCodes.GateNotFound);

        var update = Builders<Gate>.Update
            .Set(x => x.IsActive, false)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);
        await _db.Gates.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);
        return Result.Ok();
    }

    private static GateDto Map(Gate x) => new()
    {
        Id = x.Id,
        BuildingId = x.BuildingId,
        Code = x.Code,
        Name = x.Name,
        Type = x.Type,
        IsActive = x.IsActive,
        CreatedAt = x.CreatedAt,
        UpdatedAt = x.UpdatedAt
    };
}
