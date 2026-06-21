using MongoDB.Bson;
using MongoDB.Driver;
using Parking.Application.Abstractions;
using Parking.Application.Common;
using Parking.Application.DTOs;
using Parking.Application.DTOs.Realtime;
using Parking.Application.DTOs.Reservations;
using Parking.Domain.Entities;
using Parking.Domain.Enums;
using Parking.Infrastructure.Persistence;

namespace Parking.Infrastructure.Services;

public class ReservationService : IReservationService
{
    private readonly MongoDbContext _db;
    private readonly IParkingMapNotifier _notifier;

    public ReservationService(MongoDbContext db, IParkingMapNotifier notifier)
    {
        _db = db;
        _notifier = notifier;
    }

    public async Task<Result<PagedResult<ReservationDto>>> GetListAsync(
        ReservationListQuery query,
        CancellationToken ct = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : query.PageSize > 200 ? 200 : query.PageSize;

        var fb = Builders<Reservation>.Filter;
        var filters = new List<FilterDefinition<Reservation>>();
        if (!string.IsNullOrWhiteSpace(query.BuildingId)) filters.Add(fb.Eq(x => x.BuildingId, query.BuildingId));
        if (query.Status.HasValue) filters.Add(fb.Eq(x => x.Status, (ReservationStatus)query.Status.Value));
        if (!string.IsNullOrWhiteSpace(query.PlateNumber)) filters.Add(fb.Eq(x => x.PlateNumber, PlateNumberNormalizer.Normalize(query.PlateNumber)));
        if (!string.IsNullOrWhiteSpace(query.DriverUserId)) filters.Add(fb.Eq(x => x.DriverUserId, query.DriverUserId));
        if (query.ReservedFromStart.HasValue) filters.Add(fb.Gte(x => x.ReservedFrom, query.ReservedFromStart.Value));
        if (query.ReservedFromEnd.HasValue) filters.Add(fb.Lte(x => x.ReservedFrom, query.ReservedFromEnd.Value));

        var filter = filters.Count == 0 ? fb.Empty : fb.And(filters);
        var total = await _db.Reservations.CountDocumentsAsync(filter, cancellationToken: ct);

        var items = await _db.Reservations.Find(filter)
            .SortByDescending(x => x.ReservedFrom)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(ct);

        return Result<PagedResult<ReservationDto>>.Ok(new PagedResult<ReservationDto>
        {
            Items = items.Select(Map).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        });
    }

    public async Task<Result<ReservationDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var entity = await _db.Reservations.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (entity is null)
            return Result<ReservationDto>.Fail("Reservation not found.", ParkingErrorCodes.ReservationNotFound);
        return Result<ReservationDto>.Ok(Map(entity));
    }

    public async Task<Result<ReservationDto>> CreateAsync(
        CreateReservationRequest request,
        string? driverUserId,
        CancellationToken ct = default)
    {
        var validation = ValidateCreate(request);
        if (validation is not null)
            return Result<ReservationDto>.Fail(validation, ParkingErrorCodes.ValidationFailed);

        var now = DateTime.UtcNow;
        if (request.ReservedFrom >= request.ReservedTo || request.ReservedFrom < now)
            return Result<ReservationDto>.Fail(
                "ReservedFrom must be in the future and earlier than ReservedTo.",
                ParkingErrorCodes.InvalidReservationWindow);

        ParkingSlot? slot = null;
        if (!string.IsNullOrWhiteSpace(request.ParkingSlotId))
        {
            slot = await _db.ParkingSlots.Find(x => x.Id == request.ParkingSlotId).FirstOrDefaultAsync(ct);
            if (slot is null)
                return Result<ReservationDto>.Fail("Parking slot not found.", ParkingErrorCodes.SlotNotFound);
            if (slot.Status != SlotStatus.Available)
                return Result<ReservationDto>.Fail("Parking slot is not available.", ParkingErrorCodes.SlotNotAvailable);

            var hasConflict = await HasConflictAsync(request.ParkingSlotId!, request.ReservedFrom, request.ReservedTo, null, ct);
            if (hasConflict)
                return Result<ReservationDto>.Fail(
                    "Another reservation overlaps the requested window on this slot.",
                    ParkingErrorCodes.ReservationSlotConflict);
        }

        var entity = new Reservation
        {
            Id = ObjectId.GenerateNewId().ToString(),
            BuildingId = request.BuildingId.Trim(),
            VehicleTypeId = request.VehicleTypeId.Trim(),
            PlateNumber = PlateNumberNormalizer.Normalize(request.PlateNumber),
            VehicleId = string.IsNullOrWhiteSpace(request.VehicleId) ? null : request.VehicleId.Trim(),
            DriverUserId = string.IsNullOrWhiteSpace(driverUserId) ? null : driverUserId,
            ZoneId = string.IsNullOrWhiteSpace(request.ZoneId) ? slot?.ZoneId : request.ZoneId.Trim(),
            ParkingSlotId = string.IsNullOrWhiteSpace(request.ParkingSlotId) ? null : request.ParkingSlotId.Trim(),
            ReservedFrom = request.ReservedFrom,
            ReservedTo = request.ReservedTo,
            Status = ReservationStatus.Pending,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            CreatedAt = now
        };

        await _db.Reservations.InsertOneAsync(entity, cancellationToken: ct);

        if (slot is not null)
        {
            await SetSlotStatusAsync(slot, SlotStatus.Reserved, ct);
        }

        return Result<ReservationDto>.Ok(Map(entity));
    }

    public async Task<Result<ReservationDto>> UpdateAsync(
        string id,
        UpdateReservationRequest request,
        CancellationToken ct = default)
    {
        var entity = await _db.Reservations.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (entity is null)
            return Result<ReservationDto>.Fail("Reservation not found.", ParkingErrorCodes.ReservationNotFound);

        if (entity.Status != ReservationStatus.Pending && entity.Status != ReservationStatus.Confirmed)
            return Result<ReservationDto>.Fail(
                "Only pending or confirmed reservations can be updated.",
                ParkingErrorCodes.InvalidReservationStatus);

        var now = DateTime.UtcNow;
        if (request.ReservedFrom >= request.ReservedTo || request.ReservedFrom < now)
            return Result<ReservationDto>.Fail(
                "ReservedFrom must be in the future and earlier than ReservedTo.",
                ParkingErrorCodes.InvalidReservationWindow);

        if (!string.IsNullOrWhiteSpace(entity.ParkingSlotId))
        {
            var hasConflict = await HasConflictAsync(entity.ParkingSlotId!, request.ReservedFrom, request.ReservedTo, entity.Id, ct);
            if (hasConflict)
                return Result<ReservationDto>.Fail(
                    "Another reservation overlaps the requested window on this slot.",
                    ParkingErrorCodes.ReservationSlotConflict);
        }

        var update = Builders<Reservation>.Update
            .Set(x => x.ZoneId, string.IsNullOrWhiteSpace(request.ZoneId) ? entity.ZoneId : request.ZoneId.Trim())
            .Set(x => x.ReservedFrom, request.ReservedFrom)
            .Set(x => x.ReservedTo, request.ReservedTo)
            .Set(x => x.Note, string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim())
            .Set(x => x.UpdatedAt, now);

        await _db.Reservations.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);
        var updated = await _db.Reservations.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        return Result<ReservationDto>.Ok(Map(updated!));
    }

    public async Task<Result<ReservationDto>> ConfirmAsync(string id, CancellationToken ct = default)
    {
        var entity = await _db.Reservations.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (entity is null)
            return Result<ReservationDto>.Fail("Reservation not found.", ParkingErrorCodes.ReservationNotFound);

        if (entity.Status != ReservationStatus.Pending)
            return Result<ReservationDto>.Fail(
                "Only pending reservations can be confirmed.",
                ParkingErrorCodes.InvalidReservationStatus);

        var update = Builders<Reservation>.Update
            .Set(x => x.Status, ReservationStatus.Confirmed)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        await _db.Reservations.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);
        var updated = await _db.Reservations.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        return Result<ReservationDto>.Ok(Map(updated!));
    }

    public async Task<Result<ReservationDto>> CancelAsync(
        string id,
        CancelReservationRequest request,
        string? cancelledByUserId,
        bool enforceOwnership,
        CancellationToken ct = default)
    {
        var entity = await _db.Reservations.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (entity is null)
            return Result<ReservationDto>.Fail("Reservation not found.", ParkingErrorCodes.ReservationNotFound);

        // Drivers may only cancel their own reservation; staff/managers bypass this check.
        if (enforceOwnership
            && (string.IsNullOrWhiteSpace(cancelledByUserId)
                || entity.DriverUserId != cancelledByUserId))
            return Result<ReservationDto>.Fail(
                "You can only cancel your own reservation.",
                ParkingErrorCodes.ReservationAccessDenied);

        if (entity.Status != ReservationStatus.Pending && entity.Status != ReservationStatus.Confirmed)
            return Result<ReservationDto>.Fail(
                "Only pending or confirmed reservations can be cancelled.",
                ParkingErrorCodes.InvalidReservationStatus);

        var now = DateTime.UtcNow;
        var update = Builders<Reservation>.Update
            .Set(x => x.Status, ReservationStatus.Cancelled)
            .Set(x => x.CancelledByUserId, string.IsNullOrWhiteSpace(cancelledByUserId) ? null : cancelledByUserId)
            .Set(x => x.CancelledAt, now)
            .Set(x => x.UpdatedAt, now);
        if (!string.IsNullOrWhiteSpace(request.Note))
            update = update.Set(x => x.Note, request.Note.Trim());

        await _db.Reservations.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);

        await ReleaseSlotIfReservedAsync(entity, ct);

        var updated = await _db.Reservations.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        return Result<ReservationDto>.Ok(Map(updated!));
    }

    public async Task<Result<ReservationDto>> CheckInAsync(
        string id,
        CheckInReservationRequest request,
        CancellationToken ct = default)
    {
        var entity = await _db.Reservations.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (entity is null)
            return Result<ReservationDto>.Fail("Reservation not found.", ParkingErrorCodes.ReservationNotFound);

        if (entity.Status != ReservationStatus.Pending && entity.Status != ReservationStatus.Confirmed)
            return Result<ReservationDto>.Fail(
                "Only pending or confirmed reservations can be checked in.",
                ParkingErrorCodes.InvalidReservationStatus);

        var update = Builders<Reservation>.Update
            .Set(x => x.Status, ReservationStatus.CheckedIn)
            .Set(x => x.ParkingSessionId, string.IsNullOrWhiteSpace(request.ParkingSessionId) ? null : request.ParkingSessionId.Trim())
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        await _db.Reservations.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);
        var updated = await _db.Reservations.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        return Result<ReservationDto>.Ok(Map(updated!));
    }

    public async Task<Result<ReservationDto>> ExpireAsync(string id, CancellationToken ct = default)
    {
        var entity = await _db.Reservations.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (entity is null)
            return Result<ReservationDto>.Fail("Reservation not found.", ParkingErrorCodes.ReservationNotFound);

        if (entity.Status != ReservationStatus.Pending && entity.Status != ReservationStatus.Confirmed)
            return Result<ReservationDto>.Fail(
                "Only pending or confirmed reservations can expire.",
                ParkingErrorCodes.InvalidReservationStatus);

        if (DateTime.UtcNow <= entity.ReservedTo)
            return Result<ReservationDto>.Fail(
                "Reservation window has not passed yet.",
                ParkingErrorCodes.InvalidReservationWindow);

        var update = Builders<Reservation>.Update
            .Set(x => x.Status, ReservationStatus.Expired)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        await _db.Reservations.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);

        await ReleaseSlotIfReservedAsync(entity, ct);

        var updated = await _db.Reservations.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        return Result<ReservationDto>.Ok(Map(updated!));
    }

    private async Task<bool> HasConflictAsync(
        string slotId,
        DateTime from,
        DateTime to,
        string? excludeReservationId,
        CancellationToken ct)
    {
        var fb = Builders<Reservation>.Filter;
        var filters = new List<FilterDefinition<Reservation>>
        {
            fb.Eq(x => x.ParkingSlotId, slotId),
            fb.In(x => x.Status, new[] { ReservationStatus.Pending, ReservationStatus.Confirmed }),
            // Overlap: existing.From < new.To && existing.To > new.From
            fb.Lt(x => x.ReservedFrom, to),
            fb.Gt(x => x.ReservedTo, from)
        };
        if (!string.IsNullOrWhiteSpace(excludeReservationId))
            filters.Add(fb.Ne(x => x.Id, excludeReservationId));

        var count = await _db.Reservations.CountDocumentsAsync(fb.And(filters), cancellationToken: ct);
        return count > 0;
    }

    private async Task ReleaseSlotIfReservedAsync(Reservation reservation, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reservation.ParkingSlotId))
            return;

        var slot = await _db.ParkingSlots.Find(x => x.Id == reservation.ParkingSlotId).FirstOrDefaultAsync(ct);
        if (slot is null || slot.Status != SlotStatus.Reserved)
            return;

        await SetSlotStatusAsync(slot, SlotStatus.Available, ct);
    }

    private async Task SetSlotStatusAsync(ParkingSlot slot, SlotStatus status, CancellationToken ct)
    {
        var update = Builders<ParkingSlot>.Update
            .Set(x => x.Status, status)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);
        await _db.ParkingSlots.UpdateOneAsync(x => x.Id == slot.Id, update, cancellationToken: ct);

        await _notifier.NotifySlotChangedAsync(new SlotStatusChangedEvent
        {
            FloorId = slot.FloorId,
            BuildingId = slot.BuildingId,
            SlotId = slot.Id,
            SlotCode = slot.Code,
            Status = status,
            Vehicle = null,
            OccurredAt = DateTime.UtcNow
        }, ct);
    }

    private static string? ValidateCreate(CreateReservationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.BuildingId)) return "BuildingId is required.";
        if (string.IsNullOrWhiteSpace(request.VehicleTypeId)) return "VehicleTypeId is required.";
        if (string.IsNullOrWhiteSpace(request.PlateNumber)) return "PlateNumber is required.";
        return null;
    }

    private static ReservationDto Map(Reservation x) => new()
    {
        Id = x.Id,
        BuildingId = x.BuildingId,
        VehicleTypeId = x.VehicleTypeId,
        PlateNumber = x.PlateNumber,
        VehicleId = x.VehicleId,
        DriverUserId = x.DriverUserId,
        ZoneId = x.ZoneId,
        ParkingSlotId = x.ParkingSlotId,
        ReservedFrom = x.ReservedFrom,
        ReservedTo = x.ReservedTo,
        Status = x.Status,
        ParkingSessionId = x.ParkingSessionId,
        CancelledByUserId = x.CancelledByUserId,
        CancelledAt = x.CancelledAt,
        Note = x.Note,
        CreatedAt = x.CreatedAt,
        UpdatedAt = x.UpdatedAt
    };
}
