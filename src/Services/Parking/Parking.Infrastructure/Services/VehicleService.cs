using MongoDB.Bson;
using MongoDB.Driver;
using Parking.Application.Abstractions;
using Parking.Application.Common;
using Parking.Application.DTOs;
using Parking.Application.DTOs.Vehicles;
using Parking.Domain.Entities;
using Parking.Domain.Enums;
using Parking.Infrastructure.Persistence;

namespace Parking.Infrastructure.Services;

public class VehicleService : IVehicleService
{
    private readonly MongoDbContext _db;

    public VehicleService(MongoDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PagedResult<VehicleDto>>> GetListAsync(
        string? search,
        string? ownerUserId,
        string? vehicleTypeId,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 200) pageSize = 200;

        var fb = Builders<Vehicle>.Filter;
        var filters = new List<FilterDefinition<Vehicle>>();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = Normalize(search);
            filters.Add(fb.Regex(x => x.PlateNumberNormalized, new BsonRegularExpression(normalized, "i")));
        }
        if (!string.IsNullOrWhiteSpace(ownerUserId)) filters.Add(fb.Eq(x => x.OwnerUserId, ownerUserId));
        if (!string.IsNullOrWhiteSpace(vehicleTypeId)) filters.Add(fb.Eq(x => x.VehicleTypeId, vehicleTypeId));
        if (isActive.HasValue) filters.Add(fb.Eq(x => x.IsActive, isActive.Value));

        var filter = filters.Count == 0 ? fb.Empty : fb.And(filters);
        var total = await _db.Vehicles.CountDocumentsAsync(filter, cancellationToken: ct);

        var items = await _db.Vehicles.Find(filter)
            .SortByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(ct);

        var dtos = new List<VehicleDto>();
        foreach (var item in items)
            dtos.Add(await MapAsync(item));

        return Result<PagedResult<VehicleDto>>.Ok(new PagedResult<VehicleDto>
        {
            Items = dtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        });
    }

    public async Task<Result<VehicleDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var entity = await _db.Vehicles.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (entity is null)
            return Result<VehicleDto>.Fail("Vehicle not found.", ParkingErrorCodes.VehicleNotFound);
        return Result<VehicleDto>.Ok(await MapAsync(entity));
    }

    public async Task<Result<VehicleDto>> GetByPlateAsync(string plateNumber, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(plateNumber))
            return Result<VehicleDto>.Fail("Plate number is required.", ParkingErrorCodes.ValidationFailed);

        var normalized = Normalize(plateNumber);
        var entity = await _db.Vehicles.Find(x => x.PlateNumberNormalized == normalized).FirstOrDefaultAsync(ct);
        if (entity is null)
            return Result<VehicleDto>.Fail("Vehicle not found.", ParkingErrorCodes.VehicleNotFound);
        return Result<VehicleDto>.Ok(await MapAsync(entity));
    }

    public async Task<Result<VehicleDto>> CreateAsync(CreateVehicleRequest request, CancellationToken ct = default)
    {
        var validation = ValidateCreate(request);
        if (validation is not null)
            return Result<VehicleDto>.Fail(validation, ParkingErrorCodes.ValidationFailed);

        var normalized = Normalize(request.PlateNumber);
        var exists = await _db.Vehicles.Find(x => x.PlateNumberNormalized == normalized).AnyAsync(ct);
        if (exists)
            return Result<VehicleDto>.Fail("A vehicle with the same plate number already exists.", ParkingErrorCodes.DuplicatePlateNumber);

        var entity = new Vehicle
        {
            Id = ObjectId.GenerateNewId().ToString(),
            PlateNumber = request.PlateNumber.Trim(),
            PlateNumberNormalized = normalized,
            VehicleTypeId = request.VehicleTypeId.Trim(),
            OwnerUserId = request.OwnerUserId?.Trim(),
            OwnerName = request.OwnerName?.Trim(),
            OwnerPhone = request.OwnerPhone?.Trim(),
            OwnerEmail = request.OwnerEmail?.Trim(),
            Brand = request.Brand?.Trim(),
            Model = request.Model?.Trim(),
            Color = request.Color?.Trim(),
            Note = request.Note?.Trim(),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _db.Vehicles.InsertOneAsync(entity, cancellationToken: ct);
        return Result<VehicleDto>.Ok(await MapAsync(entity));
    }

    public async Task<Result<VehicleDto>> UpdateAsync(string id, UpdateVehicleRequest request, CancellationToken ct = default)
    {
        var entity = await _db.Vehicles.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (entity is null)
            return Result<VehicleDto>.Fail("Vehicle not found.", ParkingErrorCodes.VehicleNotFound);

        if (string.IsNullOrWhiteSpace(request.VehicleTypeId))
            return Result<VehicleDto>.Fail("VehicleTypeId is required.", ParkingErrorCodes.ValidationFailed);

        var update = Builders<Vehicle>.Update
            .Set(x => x.VehicleTypeId, request.VehicleTypeId.Trim())
            .Set(x => x.OwnerUserId, request.OwnerUserId?.Trim())
            .Set(x => x.OwnerName, request.OwnerName?.Trim())
            .Set(x => x.OwnerPhone, request.OwnerPhone?.Trim())
            .Set(x => x.OwnerEmail, request.OwnerEmail?.Trim())
            .Set(x => x.Brand, request.Brand?.Trim())
            .Set(x => x.Model, request.Model?.Trim())
            .Set(x => x.Color, request.Color?.Trim())
            .Set(x => x.Note, request.Note?.Trim())
            .Set(x => x.IsActive, request.IsActive)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        await _db.Vehicles.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);
        var updated = await _db.Vehicles.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        return Result<VehicleDto>.Ok(await MapAsync(updated!));
    }

    public async Task<Result> DeleteAsync(string id, CancellationToken ct = default)
    {
        var entity = await _db.Vehicles.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (entity is null)
            return Result.Fail("Vehicle not found.", ParkingErrorCodes.VehicleNotFound);

        var update = Builders<Vehicle>.Update
            .Set(x => x.IsActive, false)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);
        await _db.Vehicles.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);
        return Result.Ok();
    }

    private static string? ValidateCreate(CreateVehicleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PlateNumber)) return "PlateNumber is required.";
        if (string.IsNullOrWhiteSpace(request.VehicleTypeId)) return "VehicleTypeId is required.";
        return null;
    }

    private static string Normalize(string plate) =>
        new string(plate.Where(c => c != ' ' && c != '-').ToArray()).ToUpperInvariant();

    private async Task<VehicleDto> MapAsync(Vehicle x)
    {
        var vt = await _db.VehicleTypes.Find(t => t.Id == x.VehicleTypeId).FirstOrDefaultAsync();
        return new VehicleDto
        {
            Id = x.Id,
            PlateNumber = x.PlateNumber,
            PlateNumberNormalized = x.PlateNumberNormalized,
            VehicleTypeId = x.VehicleTypeId,
            VehicleCategory = vt?.Category ?? VehicleCategory.Motorcycle,
            OwnerUserId = x.OwnerUserId,
            OwnerName = x.OwnerName,
            OwnerPhone = x.OwnerPhone,
            OwnerEmail = x.OwnerEmail,
            Brand = x.Brand,
            Model = x.Model,
            Color = x.Color,
            ActiveSubscriptionId = x.ActiveSubscriptionId,
            Note = x.Note,
            IsActive = x.IsActive,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt
        };
    }
}
