using MongoDB.Bson;
using MongoDB.Driver;
using Parking.Application.Abstractions;
using Parking.Application.Common;
using Parking.Application.DTOs;
using Parking.Application.DTOs.Realtime;
using Parking.Application.DTOs.Sessions;
using Parking.Domain.Entities;
using Parking.Domain.Enums;
using Parking.Infrastructure.Persistence;

namespace Parking.Infrastructure.Services;

public class ParkingSessionService : IParkingSessionService
{
    private readonly MongoDbContext _db;
    private readonly IParkingMapNotifier _notifier;
    private readonly ISlotAllocationService _slotAllocation;
    private readonly IFeeCalculationClient _feeCalculation;
    private readonly ISubscriptionCheckClient _subCheck;

    public ParkingSessionService(
        MongoDbContext db,
        IParkingMapNotifier notifier,
        ISlotAllocationService slotAllocation,
        IFeeCalculationClient feeCalculation,
        ISubscriptionCheckClient subCheck)
    {
        _db = db;
        _notifier = notifier;
        _slotAllocation = slotAllocation;
        _feeCalculation = feeCalculation;
        _subCheck = subCheck;
    }

    public async Task<Result<PagedResult<ParkingSessionDto>>> GetListAsync(
        SessionListQuery query,
        CancellationToken ct = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : query.PageSize;
        if (pageSize > 200) pageSize = 200;

        var fb = Builders<ParkingSession>.Filter;
        var filters = new List<FilterDefinition<ParkingSession>>();

        if (!string.IsNullOrWhiteSpace(query.BuildingId))
            filters.Add(fb.Eq(x => x.BuildingId, query.BuildingId));
        if (query.Status.HasValue)
            filters.Add(fb.Eq(x => x.Status, (ParkingSessionStatus)query.Status.Value));
        if (!string.IsNullOrWhiteSpace(query.PlateNumber))
        {
            var plate = query.PlateNumber.Trim();
            filters.Add(fb.Regex(x => x.PlateNumber, new BsonRegularExpression(plate, "i")));
        }
        if (query.PlateNumbers is { Count: > 0 })
        {
            // Lọc theo tập biển số (lượt gửi của tôi). Khớp không phân biệt hoa thường.
            var plates = query.PlateNumbers
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => new BsonRegularExpression($"^{System.Text.RegularExpressions.Regex.Escape(p.Trim())}$", "i"))
                .ToList();
            if (plates.Count > 0)
                filters.Add(fb.Or(plates.Select(p => fb.Regex(x => x.PlateNumber, p))));
        }
        if (query.FromDate.HasValue)
            filters.Add(fb.Gte(x => x.CheckInTime, query.FromDate.Value));
        if (query.ToDate.HasValue)
            filters.Add(fb.Lte(x => x.CheckInTime, query.ToDate.Value));

        var filter = filters.Count == 0 ? fb.Empty : fb.And(filters);
        var total = await _db.ParkingSessions.CountDocumentsAsync(filter, cancellationToken: ct);

        var items = await _db.ParkingSessions.Find(filter)
            .SortByDescending(x => x.CheckInTime)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(ct);

        return Result<PagedResult<ParkingSessionDto>>.Ok(new PagedResult<ParkingSessionDto>
        {
            Items = items.Select(Map).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        });
    }

    public async Task<Result<ParkingSessionDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var entity = await _db.ParkingSessions.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (entity is null)
            return Result<ParkingSessionDto>.Fail("Parking session not found.", ParkingErrorCodes.SessionNotFound);
        return Result<ParkingSessionDto>.Ok(Map(entity));
    }

    public async Task<Result<PagedResult<ParkingSessionDto>>> GetMySessionsAsync(
        string driverUserId, int page, int pageSize, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize;
        if (pageSize > 200) pageSize = 200;

        // Lấy biển số (cả dạng gốc và dạng chuẩn hoá) của các xe driver đã đăng ký.
        // Session lưu biển đã chuẩn hoá (bỏ dấu cách/gạch) nên cần khớp cả hai dạng.
        var vehicles = await _db.Vehicles
            .Find(v => v.OwnerUserId == driverUserId && v.IsActive)
            .Project(v => new { v.PlateNumber, v.PlateNumberNormalized })
            .ToListAsync(ct);

        var plates = vehicles
            .SelectMany(v => new[] { v.PlateNumber, v.PlateNumberNormalized })
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct()
            .ToList();

        if (plates.Count == 0)
        {
            return Result<PagedResult<ParkingSessionDto>>.Ok(new PagedResult<ParkingSessionDto>
            {
                Items = new List<ParkingSessionDto>(),
                Page = page,
                PageSize = pageSize,
                TotalCount = 0
            });
        }

        return await GetListAsync(new SessionListQuery
        {
            PlateNumbers = plates,
            Page = page,
            PageSize = pageSize
        }, ct);
    }

    public async Task<Result<ParkingSessionDto>> GetActiveByPlateAsync(string plateNumber, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(plateNumber))
            return Result<ParkingSessionDto>.Fail("Plate number is required.", ParkingErrorCodes.ValidationFailed);

        var plate = PlateNumberNormalizer.Normalize(plateNumber);
        var entity = await _db.ParkingSessions
            .Find(x => x.PlateNumber == plate && x.Status == ParkingSessionStatus.Active)
            .FirstOrDefaultAsync(ct);
        if (entity is null)
            return Result<ParkingSessionDto>.Fail("No active session found for this plate.", ParkingErrorCodes.SessionNotFound);
        return Result<ParkingSessionDto>.Ok(Map(entity));
    }

    public async Task<Result<ParkingSessionDto>> CheckInAsync(
        CheckInRequest request,
        string userId,
        CancellationToken ct = default)
    {
        var validation = ValidateCheckIn(request);
        if (validation is not null)
            return Result<ParkingSessionDto>.Fail(validation, ParkingErrorCodes.ValidationFailed);

        var plate = PlateNumberNormalizer.Normalize(request.PlateNumber);

        // 1. Zone Capacity Check
        var zone = await _db.Zones.Find(x => x.Id == request.ZoneId.Trim()).FirstOrDefaultAsync(ct);
        if (zone is null)
            return Result<ParkingSessionDto>.Fail("Zone not found.", ParkingErrorCodes.ZoneNotFound);

        if (zone.CurrentOccupancy >= zone.Capacity)
            return Result<ParkingSessionDto>.Fail("Zone is at maximum capacity.", ParkingErrorCodes.ZoneFull);

        // 2. Reject if an Active session already exists for this plate.
        var existing = await _db.ParkingSessions
            .Find(x => x.PlateNumber == plate && x.Status == ParkingSessionStatus.Active)
            .AnyAsync(ct);
        if (existing)
            return Result<ParkingSessionDto>.Fail(
                "An active session already exists for this plate number.",
                ParkingErrorCodes.ActiveSessionExists);

        // 3. Monthly Subscription Check & Auto-Detection
        var activeSub = await _subCheck.GetActiveByPlateAsync(plate, ct);
        var isMonthly = request.IsMonthly;
        string? subscriptionId = request.SubscriptionId?.Trim();

        if (activeSub != null)
        {
            isMonthly = true;
            subscriptionId ??= activeSub.Id;
        }
        else if (request.IsMonthly)
        {
            return Result<ParkingSessionDto>.Fail(
                "No active monthly subscription found for this plate number.",
                ParkingErrorCodes.InvalidSubscription);
        }

        // If checking in against a reservation, load it and validate.
        Reservation? reservation = null;
        if (!string.IsNullOrWhiteSpace(request.ReservationId))
        {
            reservation = await _db.Reservations.Find(x => x.Id == request.ReservationId).FirstOrDefaultAsync(ct);
            if (reservation is null)
                return Result<ParkingSessionDto>.Fail("Reservation not found.", ParkingErrorCodes.ReservationNotFound);
            if (reservation.Status != ReservationStatus.Pending && reservation.Status != ReservationStatus.Confirmed)
                return Result<ParkingSessionDto>.Fail(
                    "Reservation is not in a check-in-able state.",
                    ParkingErrorCodes.InvalidReservationStatus);
        }

        // 4. Select a slot with atomic occupation retry loop
        ParkingSlot? slot = null;
        var now = DateTime.UtcNow;

        const int maxAttempts = 3;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var explicitSlotId = request.ParkingSlotId ?? reservation?.ParkingSlotId;
            if (!string.IsNullOrWhiteSpace(explicitSlotId))
            {
                slot = await _db.ParkingSlots.Find(x => x.Id == explicitSlotId).FirstOrDefaultAsync(ct);
                if (slot is null)
                    return Result<ParkingSessionDto>.Fail("Parking slot not found.", ParkingErrorCodes.SlotNotFound);

                var reservedForThis = slot.Status == SlotStatus.Reserved
                    && reservation is not null
                    && reservation.ParkingSlotId == slot.Id;
                if (slot.Status != SlotStatus.Available && !reservedForThis)
                    return Result<ParkingSessionDto>.Fail("The requested slot is not available.", ParkingErrorCodes.SlotNotAvailable);

                if (!string.IsNullOrWhiteSpace(slot.VehicleTypeId)
                    && slot.VehicleTypeId != request.VehicleTypeId.Trim())
                    return Result<ParkingSessionDto>.Fail(
                        "The slot does not match the vehicle type.",
                        ParkingErrorCodes.ValidationFailed);
            }
            else
            {
                var pick = await _slotAllocation.PickBestSlotIdAsync(
                    request.ZoneId, request.VehicleTypeId.Trim(), ct);
                if (!pick.Success)
                    return Result<ParkingSessionDto>.Fail(pick.Error!, pick.ErrorCode!);

                slot = await _db.ParkingSlots.Find(x => x.Id == pick.Value).FirstOrDefaultAsync(ct);
                if (slot is null)
                    return Result<ParkingSessionDto>.Fail("No available slot in the requested zone.", ParkingErrorCodes.NoAvailableSlot);
            }

            // Atomic update check: Only claim slot if its status is still Available or Reserved (for reservation)
            var allowedStatuses = new List<SlotStatus> { SlotStatus.Available };
            if (reservation is not null && reservation.ParkingSlotId == slot.Id)
                allowedStatuses.Add(SlotStatus.Reserved);

            var tempSessionId = ObjectId.GenerateNewId().ToString();
            var slotFilter = Builders<ParkingSlot>.Filter.And(
                Builders<ParkingSlot>.Filter.Eq(x => x.Id, slot.Id),
                Builders<ParkingSlot>.Filter.In(x => x.Status, allowedStatuses));

            var slotUpdate = Builders<ParkingSlot>.Update
                .Set(x => x.Status, SlotStatus.Occupied)
                .Set(x => x.CurrentSessionId, tempSessionId)
                .Set(x => x.UpdatedAt, now);

            var updateResult = await _db.ParkingSlots.UpdateOneAsync(slotFilter, slotUpdate, cancellationToken: ct);
            if (updateResult.ModifiedCount > 0)
            {
                // Atomically claimed the slot! Now insert session.
                var session = new ParkingSession
                {
                    Id = tempSessionId,
                    PlateNumber = plate,
                    VehicleTypeId = request.VehicleTypeId.Trim(),
                    BuildingId = request.BuildingId.Trim(),
                    ZoneId = request.ZoneId.Trim(),
                    ParkingSlotId = slot.Id,
                    ReservationId = reservation?.Id,
                    CheckInTime = now,
                    EntryGate = request.EntryGate?.Trim(),
                    CheckInNote = request.CheckInNote?.Trim(),
                    Status = ParkingSessionStatus.Active,
                    IsMonthly = isMonthly,
                    SubscriptionId = subscriptionId,
                    CreatedByUserId = userId ?? string.Empty,
                    CreatedAt = now
                };
                await _db.ParkingSessions.InsertOneAsync(session, cancellationToken: ct);

                // Increment zone occupancy.
                await _db.Zones.UpdateOneAsync(
                    x => x.Id == session.ZoneId,
                    Builders<Zone>.Update.Inc(x => x.CurrentOccupancy, 1).Set(x => x.UpdatedAt, now),
                    cancellationToken: ct);

                // If from a reservation, mark it checked-in and link the session.
                if (reservation is not null)
                {
                    await _db.Reservations.UpdateOneAsync(
                        x => x.Id == reservation.Id,
                        Builders<Reservation>.Update
                            .Set(x => x.Status, ReservationStatus.CheckedIn)
                            .Set(x => x.ParkingSessionId, session.Id)
                            .Set(x => x.UpdatedAt, now),
                        cancellationToken: ct);

                    if (!string.IsNullOrWhiteSpace(reservation.ParkingSlotId)
                        && reservation.ParkingSlotId != slot.Id)
                    {
                        var oldReserved = await _db.ParkingSlots
                            .Find(x => x.Id == reservation.ParkingSlotId)
                            .FirstOrDefaultAsync(ct);
                        if (oldReserved is not null && oldReserved.Status == SlotStatus.Reserved)
                        {
                            await _db.ParkingSlots.UpdateOneAsync(
                                x => x.Id == oldReserved.Id,
                                Builders<ParkingSlot>.Update
                                    .Set(x => x.Status, SlotStatus.Available)
                                    .Set(x => x.UpdatedAt, now),
                                cancellationToken: ct);
                            await NotifyAsync(oldReserved, SlotStatus.Available, null, ct);
                        }
                    }
                }

                // Log.
                await WriteLogAsync(session.Id, ParkingSessionLogAction.CheckIn, null, slot.Id,
                    $"Check-in plate {plate} into slot {slot.Code}.", userId, now, ct);

                // Notify realtime.
                await NotifyAsync(slot, SlotStatus.Occupied, BuildOccupyingVehicle(session), ct);

                return Result<ParkingSessionDto>.Ok(Map(session));
            }

            // If an explicit slot was requested and atomic update failed, reject immediately.
            if (!string.IsNullOrWhiteSpace(request.ParkingSlotId) || reservation != null)
            {
                return Result<ParkingSessionDto>.Fail(
                    "The requested slot was taken by another vehicle.",
                    ParkingErrorCodes.SlotNotAvailable);
            }

            // Retry auto-allocation if race condition happened.
        }

        return Result<ParkingSessionDto>.Fail(
            "Failed to allocate slot due to concurrent check-ins. Please try again.",
            ParkingErrorCodes.NoAvailableSlot);
    }

    public async Task<Result<ParkingSessionDto>> CheckOutAsync(
        string id,
        CheckOutRequest request,
        string userId,
        CancellationToken ct = default)
    {
        var session = await _db.ParkingSessions.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (session is null)
            return Result<ParkingSessionDto>.Fail("Parking session not found.", ParkingErrorCodes.SessionNotFound);
        if (session.Status != ParkingSessionStatus.Active)
            return Result<ParkingSessionDto>.Fail("Session is not active.", ParkingErrorCodes.SessionNotActive);

        var now = DateTime.UtcNow;
        var totalFee = 0m;
        if (!session.IsMonthly || request.IsLostTicket)
        {
            var feeResult = await _feeCalculation.CalculateAsync(
                session.BuildingId,
                session.VehicleTypeId,
                session.CheckInTime,
                now,
                request.IsLostTicket,
                ct);
            if (!feeResult.Success)
                return Result<ParkingSessionDto>.Fail(feeResult.Error!, feeResult.ErrorCode!);

            totalFee = feeResult.Value!.Amount;
            if (totalFee < 0)
                return Result<ParkingSessionDto>.Fail("Calculated fee must be non-negative.", ParkingErrorCodes.ValidationFailed);
        }

        var sessionUpdate = Builders<ParkingSession>.Update
            .Set(x => x.CheckOutTime, now)
            .Set(x => x.Status, request.IsLostTicket
                ? ParkingSessionStatus.LostTicket
                : ParkingSessionStatus.Completed)
            .Set(x => x.TotalFee, totalFee)
            .Set(x => x.ExitGate, request.ExitGate?.Trim())
            .Set(x => x.CheckOutNote, request.CheckOutNote?.Trim())
            .Set(x => x.PaymentId, request.PaymentId?.Trim())
            .Set(x => x.CompletedByUserId, userId ?? string.Empty)
            .Set(x => x.UpdatedAt, now);
        await _db.ParkingSessions.UpdateOneAsync(x => x.Id == id, sessionUpdate, cancellationToken: ct);

        // Free the slot.
        var slot = await _db.ParkingSlots.Find(x => x.Id == session.ParkingSlotId).FirstOrDefaultAsync(ct);
        if (slot is not null)
        {
            var slotUpdate = Builders<ParkingSlot>.Update
                .Set(x => x.Status, SlotStatus.Available)
                .Set(x => x.CurrentSessionId, (string?)null)
                .Set(x => x.UpdatedAt, now);
            await _db.ParkingSlots.UpdateOneAsync(x => x.Id == slot.Id, slotUpdate, cancellationToken: ct);
        }

        // Decrement zone occupancy.
        await _db.Zones.UpdateOneAsync(
            x => x.Id == session.ZoneId,
            Builders<Zone>.Update.Inc(x => x.CurrentOccupancy, -1).Set(x => x.UpdatedAt, now),
            cancellationToken: ct);

        // Log.
        await WriteLogAsync(session.Id,
            request.IsLostTicket ? ParkingSessionLogAction.LostTicket : ParkingSessionLogAction.CheckOut,
            session.ParkingSlotId, null,
            request.IsLostTicket
                ? $"Check-out plate {session.PlateNumber} with LOST TICKET. Fee {totalFee:0.##}."
                : $"Check-out plate {session.PlateNumber}. Fee {totalFee:0.##}.",
            userId, now, ct);

        // Notify realtime.
        if (slot is not null)
            await NotifyAsync(slot, SlotStatus.Available, null, ct);

        var updated = await _db.ParkingSessions.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        return Result<ParkingSessionDto>.Ok(Map(updated!));
    }

    public async Task<Result<EstimateFeeResponse>> EstimateFeeAsync(
        string id,
        string? requestUserId,
        bool enforceOwnership,
        CancellationToken ct = default)
    {
        var session = await _db.ParkingSessions.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (session is null)
            return Result<EstimateFeeResponse>.Fail("Parking session not found.", ParkingErrorCodes.SessionNotFound);
        if (session.Status != ParkingSessionStatus.Active)
            return Result<EstimateFeeResponse>.Fail("Session is not active.", ParkingErrorCodes.SessionNotActive);

        // Driver chỉ được xem phí tạm tính của lượt gửi thuộc xe mình đã đăng ký.
        if (enforceOwnership)
        {
            var owns = await DriverOwnsSessionAsync(session, requestUserId, ct);
            if (!owns)
                return Result<EstimateFeeResponse>.Fail(
                    "You can only view your own session's estimated fee.",
                    ParkingErrorCodes.SessionAccessDenied);
        }

        var now = DateTime.UtcNow;
        var response = new EstimateFeeResponse
        {
            SessionId = session.Id,
            PlateNumber = session.PlateNumber,
            VehicleTypeId = session.VehicleTypeId,
            CheckInTime = session.CheckInTime,
            EstimatedAt = now,
            Duration = now - session.CheckInTime,
            IsMonthly = session.IsMonthly
        };

        // Vé tháng không phát sinh phí lượt.
        if (session.IsMonthly)
            return Result<EstimateFeeResponse>.Ok(response);

        var feeResult = await _feeCalculation.CalculateAsync(
            session.BuildingId,
            session.VehicleTypeId,
            session.CheckInTime,
            now,
            false,
            ct);
        if (!feeResult.Success)
            return Result<EstimateFeeResponse>.Fail(feeResult.Error!, feeResult.ErrorCode!);

        response.EstimatedFee = feeResult.Value!.Amount;
        response.FeePolicyId = feeResult.Value!.FeePolicyId;
        return Result<EstimateFeeResponse>.Ok(response);
    }

    /// <summary>
    /// True nếu phiên gửi thuộc một xe mà driver đã đăng ký (so khớp biển số cả dạng
    /// gốc và dạng chuẩn hoá). Dùng để chặn driver xem dữ liệu phiên của người khác.
    /// </summary>
    private async Task<bool> DriverOwnsSessionAsync(
        ParkingSession session,
        string? driverUserId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(driverUserId))
            return false;

        var vehicles = await _db.Vehicles
            .Find(v => v.OwnerUserId == driverUserId && v.IsActive)
            .Project(v => new { v.PlateNumber, v.PlateNumberNormalized })
            .ToListAsync(ct);

        var sessionPlate = PlateNumberNormalizer.Normalize(session.PlateNumber);
        return vehicles.Any(v =>
            PlateNumberNormalizer.Normalize(v.PlateNumber) == sessionPlate
            || PlateNumberNormalizer.Normalize(v.PlateNumberNormalized) == sessionPlate);
    }

    public async Task<Result<ParkingSessionDto>> ChangeSlotAsync(
        string id,
        ChangeSlotRequest request,
        string userId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.NewParkingSlotId))
            return Result<ParkingSessionDto>.Fail("NewParkingSlotId is required.", ParkingErrorCodes.ValidationFailed);

        var session = await _db.ParkingSessions.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (session is null)
            return Result<ParkingSessionDto>.Fail("Parking session not found.", ParkingErrorCodes.SessionNotFound);
        if (session.Status != ParkingSessionStatus.Active)
            return Result<ParkingSessionDto>.Fail("Session is not active.", ParkingErrorCodes.SessionNotActive);

        if (request.NewParkingSlotId == session.ParkingSlotId)
            return Result<ParkingSessionDto>.Fail("New slot is the same as the current slot.", ParkingErrorCodes.ValidationFailed);

        var newSlot = await _db.ParkingSlots.Find(x => x.Id == request.NewParkingSlotId).FirstOrDefaultAsync(ct);
        if (newSlot is null)
            return Result<ParkingSessionDto>.Fail("Target parking slot not found.", ParkingErrorCodes.SlotNotFound);
        if (newSlot.Status != SlotStatus.Available)
            return Result<ParkingSessionDto>.Fail("The target slot is not available.", ParkingErrorCodes.SlotNotAvailable);
        if (!string.IsNullOrWhiteSpace(newSlot.VehicleTypeId)
            && newSlot.VehicleTypeId != session.VehicleTypeId)
            return Result<ParkingSessionDto>.Fail(
                "The target slot does not match the vehicle type.",
                ParkingErrorCodes.ValidationFailed);

        var now = DateTime.UtcNow;
        var oldSlotId = session.ParkingSlotId;
        var oldSlot = await _db.ParkingSlots.Find(x => x.Id == oldSlotId).FirstOrDefaultAsync(ct);

        // Free old slot.
        if (oldSlot is not null)
        {
            var freeUpdate = Builders<ParkingSlot>.Update
                .Set(x => x.Status, SlotStatus.Available)
                .Set(x => x.CurrentSessionId, (string?)null)
                .Set(x => x.UpdatedAt, now);
            await _db.ParkingSlots.UpdateOneAsync(x => x.Id == oldSlotId, freeUpdate, cancellationToken: ct);
        }

        // Occupy new slot.
        var occupyUpdate = Builders<ParkingSlot>.Update
            .Set(x => x.Status, SlotStatus.Occupied)
            .Set(x => x.CurrentSessionId, session.Id)
            .Set(x => x.UpdatedAt, now);
        await _db.ParkingSlots.UpdateOneAsync(x => x.Id == newSlot.Id, occupyUpdate, cancellationToken: ct);

        // If the new slot belongs to a different zone, move occupancy and update the
        // session's zone/building so check-out later decrements the correct zone.
        var zoneChanged = !string.IsNullOrWhiteSpace(newSlot.ZoneId) && newSlot.ZoneId != session.ZoneId;
        if (zoneChanged)
        {
            await _db.Zones.UpdateOneAsync(
                x => x.Id == session.ZoneId,
                Builders<Zone>.Update.Inc(x => x.CurrentOccupancy, -1).Set(x => x.UpdatedAt, now),
                cancellationToken: ct);
            await _db.Zones.UpdateOneAsync(
                x => x.Id == newSlot.ZoneId,
                Builders<Zone>.Update.Inc(x => x.CurrentOccupancy, 1).Set(x => x.UpdatedAt, now),
                cancellationToken: ct);
        }

        // Update session.
        var sessionUpdate = Builders<ParkingSession>.Update
            .Set(x => x.ParkingSlotId, newSlot.Id)
            .Set(x => x.UpdatedAt, now);
        if (zoneChanged)
        {
            sessionUpdate = sessionUpdate
                .Set(x => x.ZoneId, newSlot.ZoneId)
                .Set(x => x.BuildingId, newSlot.BuildingId);
        }
        await _db.ParkingSessions.UpdateOneAsync(x => x.Id == id, sessionUpdate, cancellationToken: ct);

        // Log.
        await WriteLogAsync(session.Id, ParkingSessionLogAction.ChangeSlot, oldSlotId, newSlot.Id,
            $"Change slot for plate {session.PlateNumber} to {newSlot.Code}.", userId, now, ct);

        // Notify realtime for both slots.
        if (oldSlot is not null)
            await NotifyAsync(oldSlot, SlotStatus.Available, null, ct);

        var updated = await _db.ParkingSessions.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        await NotifyAsync(newSlot, SlotStatus.Occupied, BuildOccupyingVehicle(updated!), ct);

        return Result<ParkingSessionDto>.Ok(Map(updated!));
    }

    public async Task<Result<ParkingSessionDto>> UpdateInfoAsync(
        string id, UpdateSessionInfoRequest request, string userId, CancellationToken ct = default)
    {
        var session = await _db.ParkingSessions.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (session is null)
            return Result<ParkingSessionDto>.Fail("Parking session not found.", ParkingErrorCodes.SessionNotFound);
        if (session.Status != ParkingSessionStatus.Active)
            return Result<ParkingSessionDto>.Fail("Session is not active.", ParkingErrorCodes.SessionNotActive);

        var now = DateTime.UtcNow;
        var update = Builders<ParkingSession>.Update.Set(x => x.UpdatedAt, now);
        var changes = new List<string>();

        if (!string.IsNullOrWhiteSpace(request.PlateNumber) && request.PlateNumber.Trim() != session.PlateNumber)
        {
            update = update.Set(x => x.PlateNumber, request.PlateNumber.Trim());
            changes.Add($"biển số → {request.PlateNumber.Trim()}");
        }
        if (!string.IsNullOrWhiteSpace(request.VehicleTypeId) && request.VehicleTypeId != session.VehicleTypeId)
        {
            update = update.Set(x => x.VehicleTypeId, request.VehicleTypeId);
            changes.Add("loại xe");
        }
        if (changes.Count == 0)
            return Result<ParkingSessionDto>.Fail("Không có thông tin nào thay đổi.", ParkingErrorCodes.ValidationFailed);

        await _db.ParkingSessions.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);

        var note = request.Note?.Trim();
        await WriteLogAsync(session.Id, ParkingSessionLogAction.ManualAdjustment, session.ParkingSlotId, session.ParkingSlotId,
            $"Sửa thông tin xe: {string.Join(", ", changes)}." + (note != null ? $" Ghi chú: {note}" : ""),
            userId, now, ct);

        var updated = await _db.ParkingSessions.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        // Cập nhật biển số hiển thị realtime trên slot.
        var slot = await _db.ParkingSlots.Find(x => x.Id == session.ParkingSlotId).FirstOrDefaultAsync(ct);
        if (slot is not null)
            await NotifyAsync(slot, SlotStatus.Occupied, BuildOccupyingVehicle(updated!), ct);

        return Result<ParkingSessionDto>.Ok(Map(updated!));
    }

    public async Task<Result<ParkingSessionDto>> MarkExceptionAsync(
        string id, MarkExceptionRequest request, string userId, CancellationToken ct = default)
    {
        var session = await _db.ParkingSessions.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (session is null)
            return Result<ParkingSessionDto>.Fail("Parking session not found.", ParkingErrorCodes.SessionNotFound);
        if (session.Status != ParkingSessionStatus.Active)
            return Result<ParkingSessionDto>.Fail("Session is not active.", ParkingErrorCodes.SessionNotActive);

        var now = DateTime.UtcNow;
        var note = request.Note?.Trim();
        var update = Builders<ParkingSession>.Update
            .Set(x => x.Status, ParkingSessionStatus.Exception)
            .Set(x => x.CheckInNote, note ?? session.CheckInNote)
            .Set(x => x.UpdatedAt, now);
        await _db.ParkingSessions.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);

        await WriteLogAsync(session.Id, ParkingSessionLogAction.ManualAdjustment, session.ParkingSlotId, session.ParkingSlotId,
            $"Đánh dấu ngoại lệ cho biển {session.PlateNumber}." + (note != null ? $" Ghi chú: {note}" : ""),
            userId, now, ct);

        var updated = await _db.ParkingSessions.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        return Result<ParkingSessionDto>.Ok(Map(updated!));
    }

    private async Task WriteLogAsync(
        string sessionId,
        ParkingSessionLogAction action,
        string? fromSlotId,
        string? toSlotId,
        string description,
        string? userId,
        DateTime now,
        CancellationToken ct)
    {
        var log = new ParkingSessionLog
        {
            Id = ObjectId.GenerateNewId().ToString(),
            ParkingSessionId = sessionId,
            Action = action,
            FromParkingSlotId = fromSlotId,
            ToParkingSlotId = toSlotId,
            Description = description,
            CreatedByUserId = userId ?? string.Empty,
            CreatedAt = now
        };
        await _db.ParkingSessionLogs.InsertOneAsync(log, cancellationToken: ct);
    }

    private async Task NotifyAsync(
        ParkingSlot slot,
        SlotStatus status,
        OccupyingVehicleDto? vehicle,
        CancellationToken ct)
    {
        await _notifier.NotifySlotChangedAsync(new SlotStatusChangedEvent
        {
            FloorId = slot.FloorId,
            BuildingId = slot.BuildingId,
            SlotId = slot.Id,
            SlotCode = slot.Code,
            Status = status,
            Vehicle = vehicle,
            OccurredAt = DateTime.UtcNow
        }, ct);
    }

    private static OccupyingVehicleDto BuildOccupyingVehicle(ParkingSession session) => new()
    {
        SessionId = session.Id,
        PlateNumber = session.PlateNumber,
        VehicleTypeId = session.VehicleTypeId,
        CheckInTime = session.CheckInTime,
        IsMonthly = session.IsMonthly
    };

    private static string? ValidateCheckIn(CheckInRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PlateNumber)) return "PlateNumber is required.";
        if (string.IsNullOrWhiteSpace(request.VehicleTypeId)) return "VehicleTypeId is required.";
        if (string.IsNullOrWhiteSpace(request.BuildingId)) return "BuildingId is required.";
        if (string.IsNullOrWhiteSpace(request.ZoneId)) return "ZoneId is required.";
        return null;
    }

    private static ParkingSessionDto Map(ParkingSession x) => new()
    {
        Id = x.Id,
        PlateNumber = x.PlateNumber,
        VehicleTypeId = x.VehicleTypeId,
        VehicleId = x.VehicleId,
        BuildingId = x.BuildingId,
        ZoneId = x.ZoneId,
        ParkingSlotId = x.ParkingSlotId,
        ShiftId = x.ShiftId,
        PaymentId = x.PaymentId,
        ReservationId = x.ReservationId,
        CheckInTime = x.CheckInTime,
        CheckOutTime = x.CheckOutTime,
        EntryGate = x.EntryGate,
        ExitGate = x.ExitGate,
        CheckInNote = x.CheckInNote,
        CheckOutNote = x.CheckOutNote,
        Status = x.Status,
        IsMonthly = x.IsMonthly,
        SubscriptionId = x.SubscriptionId,
        TotalFee = x.TotalFee,
        CreatedByUserId = x.CreatedByUserId,
        CompletedByUserId = x.CompletedByUserId,
        CreatedAt = x.CreatedAt,
        UpdatedAt = x.UpdatedAt
    };
}
