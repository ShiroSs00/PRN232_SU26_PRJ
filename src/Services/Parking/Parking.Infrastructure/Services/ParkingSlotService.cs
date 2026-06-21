using MongoDB.Bson;
using MongoDB.Driver;
using Parking.Application.Abstractions;
using Parking.Application.Common;
using Parking.Application.DTOs;
using Parking.Application.DTOs.Realtime;
using Parking.Application.DTOs.Slots;
using Parking.Domain.Entities;
using Parking.Domain.Enums;
using Parking.Infrastructure.Persistence;

namespace Parking.Infrastructure.Services;

public class ParkingSlotService : IParkingSlotService
{
    private readonly MongoDbContext _db;
    private readonly IParkingMapNotifier _notifier;

    public ParkingSlotService(MongoDbContext db, IParkingMapNotifier notifier)
    {
        _db = db;
        _notifier = notifier;
    }

    public async Task<Result<PagedResult<SlotDto>>> GetListAsync(
        string? buildingId,
        string? floorId,
        string? zoneId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 200) pageSize = 200;

        var fb = Builders<ParkingSlot>.Filter;
        var filters = new List<FilterDefinition<ParkingSlot>>();
        if (!string.IsNullOrWhiteSpace(buildingId)) filters.Add(fb.Eq(x => x.BuildingId, buildingId));
        if (!string.IsNullOrWhiteSpace(floorId)) filters.Add(fb.Eq(x => x.FloorId, floorId));
        if (!string.IsNullOrWhiteSpace(zoneId)) filters.Add(fb.Eq(x => x.ZoneId, zoneId));

        var filter = filters.Count == 0 ? fb.Empty : fb.And(filters);
        var total = await _db.ParkingSlots.CountDocumentsAsync(filter, cancellationToken: ct);

        var items = await _db.ParkingSlots.Find(filter)
            .SortBy(x => x.Row).ThenBy(x => x.Column)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(ct);

        return Result<PagedResult<SlotDto>>.Ok(new PagedResult<SlotDto>
        {
            Items = items.Select(Map).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        });
    }

    public async Task<Result<SlotDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var entity = await _db.ParkingSlots.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (entity is null)
            return Result<SlotDto>.Fail("Parking slot not found.", ParkingErrorCodes.SlotNotFound);
        return Result<SlotDto>.Ok(Map(entity));
    }

    public async Task<Result<SlotDto>> CreateAsync(CreateSlotRequest request, CancellationToken ct = default)
    {
        var validation = ValidateCreate(request);
        if (validation is not null)
            return Result<SlotDto>.Fail(validation, ParkingErrorCodes.ValidationFailed);

        var floor = await _db.Floors.Find(x => x.Id == request.FloorId).FirstOrDefaultAsync(ct);
        if (floor is null)
            return Result<SlotDto>.Fail("Floor not found.", ParkingErrorCodes.FloorNotFound);

        var code = request.Code.Trim();
        var dupCode = await _db.ParkingSlots
            .Find(x => x.FloorId == request.FloorId && x.Code == code)
            .AnyAsync(ct);
        if (dupCode)
            return Result<SlotDto>.Fail($"Slot code '{code}' already exists on this floor.", ParkingErrorCodes.DuplicateSlotCode);

        var posTaken = await _db.ParkingSlots
            .Find(x => x.FloorId == request.FloorId && x.Row == request.Row && x.Column == request.Column)
            .AnyAsync(ct);
        if (posTaken)
            return Result<SlotDto>.Fail($"Position ({request.Row},{request.Column}) is already taken on this floor.", ParkingErrorCodes.SlotPositionTaken);

        var now = DateTime.UtcNow;
        var entity = new ParkingSlot
        {
            Id = ObjectId.GenerateNewId().ToString(),
            BuildingId = floor.BuildingId,
            FloorId = request.FloorId,
            ZoneId = request.ZoneId.Trim(),
            VehicleTypeId = request.VehicleTypeId.Trim(),
            Code = code,
            Label = string.IsNullOrWhiteSpace(request.Label) ? null : request.Label.Trim(),
            Row = request.Row,
            Column = request.Column,
            RowSpan = request.RowSpan < 1 ? 1 : request.RowSpan,
            ColSpan = request.ColSpan < 1 ? 1 : request.ColSpan,
            Status = SlotStatus.Available,
            CreatedAt = now,
            IsActive = true
        };

        await _db.ParkingSlots.InsertOneAsync(entity, cancellationToken: ct);
        return Result<SlotDto>.Ok(Map(entity));
    }

    public async Task<Result<SlotDto>> UpdateAsync(string id, UpdateSlotRequest request, CancellationToken ct = default)
    {
        var entity = await _db.ParkingSlots.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (entity is null)
            return Result<SlotDto>.Fail("Parking slot not found.", ParkingErrorCodes.SlotNotFound);

        var code = request.Code.Trim();
        if (string.IsNullOrWhiteSpace(code))
            return Result<SlotDto>.Fail("Code is required.", ParkingErrorCodes.ValidationFailed);

        if (!string.Equals(code, entity.Code, StringComparison.Ordinal))
        {
            var dupCode = await _db.ParkingSlots
                .Find(x => x.FloorId == entity.FloorId && x.Code == code && x.Id != id)
                .AnyAsync(ct);
            if (dupCode)
                return Result<SlotDto>.Fail($"Slot code '{code}' already exists on this floor.", ParkingErrorCodes.DuplicateSlotCode);
        }

        var update = Builders<ParkingSlot>.Update
            .Set(x => x.ZoneId, request.ZoneId.Trim())
            .Set(x => x.VehicleTypeId, request.VehicleTypeId.Trim())
            .Set(x => x.Code, code)
            .Set(x => x.Label, string.IsNullOrWhiteSpace(request.Label) ? null : request.Label.Trim())
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        await _db.ParkingSlots.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);
        var updated = await _db.ParkingSlots.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        return Result<SlotDto>.Ok(Map(updated!));
    }

    public async Task<Result> DeleteAsync(string id, CancellationToken ct = default)
    {
        var entity = await _db.ParkingSlots.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (entity is null)
            return Result.Fail("Parking slot not found.", ParkingErrorCodes.SlotNotFound);

        if (entity.Status == SlotStatus.Occupied)
            return Result.Fail("Cannot delete an occupied slot.", ParkingErrorCodes.SlotNotAvailable);

        await _db.ParkingSlots.DeleteOneAsync(x => x.Id == id, ct);
        return Result.Ok();
    }

    public async Task<Result<SlotDto>> UpdatePositionAsync(string id, UpdateSlotPositionRequest request, CancellationToken ct = default)
    {
        var entity = await _db.ParkingSlots.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (entity is null)
            return Result<SlotDto>.Fail("Parking slot not found.", ParkingErrorCodes.SlotNotFound);

        var rowSpan = request.RowSpan < 1 ? 1 : request.RowSpan;
        var colSpan = request.ColSpan < 1 ? 1 : request.ColSpan;

        if (request.Row != entity.Row || request.Column != entity.Column)
        {
            var posTaken = await _db.ParkingSlots
                .Find(x => x.FloorId == entity.FloorId && x.Row == request.Row && x.Column == request.Column && x.Id != id)
                .AnyAsync(ct);
            if (posTaken)
                return Result<SlotDto>.Fail($"Position ({request.Row},{request.Column}) is already taken on this floor.", ParkingErrorCodes.SlotPositionTaken);
        }

        var update = Builders<ParkingSlot>.Update
            .Set(x => x.Row, request.Row)
            .Set(x => x.Column, request.Column)
            .Set(x => x.RowSpan, rowSpan)
            .Set(x => x.ColSpan, colSpan)
            .Set(x => x.Label, string.IsNullOrWhiteSpace(request.Label) ? entity.Label : request.Label.Trim())
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        await _db.ParkingSlots.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);
        var updated = await _db.ParkingSlots.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        return Result<SlotDto>.Ok(Map(updated!));
    }

    public async Task<Result<List<SlotDto>>> GenerateGridAsync(GenerateGridRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.FloorId))
            return Result<List<SlotDto>>.Fail("FloorId is required.", ParkingErrorCodes.ValidationFailed);
        if (request.Rows < 1 || request.Cols < 1)
            return Result<List<SlotDto>>.Fail("Rows and Cols must be at least 1.", ParkingErrorCodes.ValidationFailed);
        if (string.IsNullOrWhiteSpace(request.CodePrefix))
            return Result<List<SlotDto>>.Fail("CodePrefix is required.", ParkingErrorCodes.ValidationFailed);

        var floor = await _db.Floors.Find(x => x.Id == request.FloorId).FirstOrDefaultAsync(ct);
        if (floor is null)
            return Result<List<SlotDto>>.Fail("Floor not found.", ParkingErrorCodes.FloorNotFound);

        var startRow = request.StartRow < 1 ? 1 : request.StartRow;
        var startCol = request.StartColumn < 1 ? 1 : request.StartColumn;
        var prefix = request.CodePrefix.Trim();

        // Build the target codes/positions, then validate against existing slots before inserting.
        var newSlots = new List<ParkingSlot>();
        var codes = new List<string>();
        var positions = new List<(int Row, int Col)>();
        var now = DateTime.UtcNow;

        for (var r = 0; r < request.Rows; r++)
        {
            for (var c = 0; c < request.Cols; c++)
            {
                var row = startRow + r;
                var col = startCol + c;
                var code = $"{prefix}-{row}-{col}";
                codes.Add(code);
                positions.Add((row, col));
                newSlots.Add(new ParkingSlot
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    BuildingId = floor.BuildingId,
                    FloorId = request.FloorId,
                    ZoneId = request.ZoneId?.Trim() ?? string.Empty,
                    VehicleTypeId = request.VehicleTypeId?.Trim() ?? string.Empty,
                    Code = code,
                    Row = row,
                    Column = col,
                    RowSpan = 1,
                    ColSpan = 1,
                    Status = SlotStatus.Available,
                    CreatedAt = now,
                    IsActive = true
                });
            }
        }

        var existing = await _db.ParkingSlots
            .Find(x => x.FloorId == request.FloorId)
            .Project(x => new { x.Code, x.Row, x.Column })
            .ToListAsync(ct);

        var existingCodes = existing.Select(e => e.Code).ToHashSet();
        var existingPositions = existing.Select(e => (e.Row, e.Column)).ToHashSet();

        var dupCode = codes.FirstOrDefault(existingCodes.Contains);
        if (dupCode is not null)
            return Result<List<SlotDto>>.Fail($"Slot code '{dupCode}' already exists on this floor.", ParkingErrorCodes.DuplicateSlotCode);

        var dupPos = positions.FirstOrDefault(existingPositions.Contains);
        if (dupPos != default)
            return Result<List<SlotDto>>.Fail($"Position ({dupPos.Row},{dupPos.Col}) is already taken on this floor.", ParkingErrorCodes.SlotPositionTaken);

        await _db.ParkingSlots.InsertManyAsync(newSlots, cancellationToken: ct);
        return Result<List<SlotDto>>.Ok(newSlots.Select(Map).ToList());
    }

    public async Task<Result<SlotDto>> UpdateStatusAsync(string id, UpdateSlotStatusRequest request, CancellationToken ct = default)
    {
        var entity = await _db.ParkingSlots.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (entity is null)
            return Result<SlotDto>.Fail("Parking slot not found.", ParkingErrorCodes.SlotNotFound);

        // Manual status change is for operational states only; Occupied/Reserved are driven by sessions/reservations.
        if (request.Status is not (SlotStatus.Available or SlotStatus.Maintenance or SlotStatus.Locked))
            return Result<SlotDto>.Fail(
                "Manual status change only supports Available, Maintenance or Locked.",
                ParkingErrorCodes.ValidationFailed);

        if (entity.Status == SlotStatus.Occupied)
            return Result<SlotDto>.Fail("Cannot manually change status of an occupied slot.", ParkingErrorCodes.SlotNotAvailable);

        var update = Builders<ParkingSlot>.Update
            .Set(x => x.Status, request.Status)
            .Set(x => x.CurrentSessionId, (string?)null)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        await _db.ParkingSlots.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);
        var updated = await _db.ParkingSlots.Find(x => x.Id == id).FirstOrDefaultAsync(ct);

        // Push realtime update so connected maps reflect the manual change.
        await _notifier.NotifySlotChangedAsync(new SlotStatusChangedEvent
        {
            FloorId = updated!.FloorId,
            BuildingId = updated.BuildingId,
            SlotId = updated.Id,
            SlotCode = updated.Code,
            Status = updated.Status,
            Vehicle = null,
            OccurredAt = DateTime.UtcNow
        }, ct);

        return Result<SlotDto>.Ok(Map(updated));
    }

    private static string? ValidateCreate(CreateSlotRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FloorId)) return "FloorId is required.";
        if (string.IsNullOrWhiteSpace(request.ZoneId)) return "ZoneId is required.";
        if (string.IsNullOrWhiteSpace(request.VehicleTypeId)) return "VehicleTypeId is required.";
        if (string.IsNullOrWhiteSpace(request.Code)) return "Code is required.";
        if (request.Row < 0) return "Row must be non-negative.";
        if (request.Column < 0) return "Column must be non-negative.";
        return null;
    }

    private static SlotDto Map(ParkingSlot x) => new()
    {
        Id = x.Id,
        BuildingId = x.BuildingId,
        FloorId = x.FloorId,
        ZoneId = x.ZoneId,
        VehicleTypeId = x.VehicleTypeId,
        Code = x.Code,
        Label = x.Label,
        Row = x.Row,
        Column = x.Column,
        RowSpan = x.RowSpan,
        ColSpan = x.ColSpan,
        Status = x.Status,
        CurrentSessionId = x.CurrentSessionId,
        IsActive = x.IsActive,
        CreatedAt = x.CreatedAt,
        UpdatedAt = x.UpdatedAt
    };
}
