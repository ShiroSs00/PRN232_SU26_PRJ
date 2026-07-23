using MongoDB.Bson;
using MongoDB.Driver;
using Parking.Application.Abstractions;
using Parking.Application.Common;
using Parking.Application.DTOs;
using Parking.Application.DTOs.Shifts;
using Parking.Domain.Entities;
using Parking.Domain.Enums;
using Parking.Infrastructure.Persistence;

namespace Parking.Infrastructure.Services;

public sealed class ShiftService : IShiftService
{
    private readonly MongoDbContext _db;
    private readonly IPaymentClient _paymentClient;

    public ShiftService(MongoDbContext db, IPaymentClient paymentClient)
    {
        _db = db;
        _paymentClient = paymentClient;
    }

    public async Task<Result<PagedResult<ShiftDto>>> GetListAsync(
        ShiftListQuery query,
        CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var fb = Builders<Shift>.Filter;
        var filters = new List<FilterDefinition<Shift>>();

        if (!string.IsNullOrWhiteSpace(query.StaffUserId))
            filters.Add(fb.Eq(x => x.StaffUserId, query.StaffUserId.Trim()));
        if (!string.IsNullOrWhiteSpace(query.BuildingId))
            filters.Add(fb.Eq(x => x.BuildingId, query.BuildingId.Trim()));
        if (query.Status.HasValue)
        {
            if (!Enum.IsDefined(typeof(ShiftStatus), query.Status.Value))
                return Result<PagedResult<ShiftDto>>.Fail(
                    "Shift status is invalid.",
                    ParkingErrorCodes.ValidationFailed);
            filters.Add(fb.Eq(x => x.Status, (ShiftStatus)query.Status.Value));
        }
        if (query.FromDate.HasValue)
            filters.Add(fb.Gte(x => x.OpenedAt, query.FromDate.Value));
        if (query.ToDate.HasValue)
            filters.Add(fb.Lte(x => x.OpenedAt, query.ToDate.Value));

        var filter = filters.Count == 0 ? fb.Empty : fb.And(filters);
        var total = await _db.Shifts.CountDocumentsAsync(filter, cancellationToken: ct);
        var shifts = await _db.Shifts.Find(filter)
            .SortByDescending(x => x.OpenedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(ct);

        return Result<PagedResult<ShiftDto>>.Ok(new PagedResult<ShiftDto>
        {
            Items = shifts.Select(Map).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        });
    }

    public async Task<Result<ShiftDto>> GetByIdAsync(
        string id,
        CancellationToken ct = default)
    {
        var shift = await _db.Shifts.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        return shift is null
            ? Result<ShiftDto>.Fail("Shift not found.", ParkingErrorCodes.ShiftNotFound)
            : Result<ShiftDto>.Ok(Map(shift));
    }

    public async Task<Result<ShiftDto>> GetCurrentAsync(
        string staffUserId,
        CancellationToken ct = default)
    {
        var shift = await _db.Shifts
            .Find(x => x.StaffUserId == staffUserId &&
                       x.Status == ShiftStatus.Open &&
                       !x.IsClosing)
            .FirstOrDefaultAsync(ct);
        return shift is null
            ? Result<ShiftDto>.Fail("No open shift was found.", ParkingErrorCodes.ShiftNotFound)
            : Result<ShiftDto>.Ok(Map(shift));
    }

    public async Task<Result<ShiftDto>> OpenAsync(
        string staffUserId,
        OpenShiftRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(staffUserId))
            return Result<ShiftDto>.Fail("Staff user is required.", ParkingErrorCodes.ValidationFailed);
        if (string.IsNullOrWhiteSpace(request.BuildingId))
            return Result<ShiftDto>.Fail("BuildingId is required.", ParkingErrorCodes.ValidationFailed);

        var buildingId = request.BuildingId.Trim();
        var buildingExists = await _db.Buildings
            .Find(x => x.Id == buildingId && x.IsActive)
            .AnyAsync(ct);
        if (!buildingExists)
            return Result<ShiftDto>.Fail("Building not found.", ParkingErrorCodes.BuildingNotFound);

        var existing = await _db.Shifts
            .Find(x => x.StaffUserId == staffUserId && x.Status == ShiftStatus.Open)
            .FirstOrDefaultAsync(ct);
        if (existing is not null)
            return Result<ShiftDto>.Fail(
                "This staff member already has an open shift.",
                ParkingErrorCodes.OpenShiftAlreadyExists);

        var shift = new Shift
        {
            Id = ObjectId.GenerateNewId().ToString(),
            StaffUserId = staffUserId,
            BuildingId = buildingId,
            OpenedAt = DateTime.UtcNow,
            Status = ShiftStatus.Open
        };

        try
        {
            await _db.Shifts.InsertOneAsync(shift, cancellationToken: ct);
            return Result<ShiftDto>.Ok(Map(shift));
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return Result<ShiftDto>.Fail(
                "This staff member already has an open shift.",
                ParkingErrorCodes.OpenShiftAlreadyExists);
        }
    }

    public async Task<Result<ShiftDto>> CloseAsync(
        string id,
        string requestedByUserId,
        bool canManageAll,
        CloseShiftRequest request,
        CancellationToken ct = default)
    {
        if (request.CountedCashAmount < 0)
            return Result<ShiftDto>.Fail(
                "CountedCashAmount cannot be negative.",
                ParkingErrorCodes.ValidationFailed);

        var shift = await _db.Shifts.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (shift is null)
            return Result<ShiftDto>.Fail("Shift not found.", ParkingErrorCodes.ShiftNotFound);
        if (!canManageAll && shift.StaffUserId != requestedByUserId)
            return Result<ShiftDto>.Fail(
                "Parking staff may only close their own shift.",
                ParkingErrorCodes.ShiftAccessDenied);
        if (shift.Status == ShiftStatus.Closed)
            return Result<ShiftDto>.Ok(Map(shift));

        var lockResult = await _db.Shifts.UpdateOneAsync(
            x => x.Id == shift.Id && x.Status == ShiftStatus.Open && !x.IsClosing,
            Builders<Shift>.Update.Set(x => x.IsClosing, true),
            cancellationToken: ct);
        if (lockResult.ModifiedCount == 0)
            return Result<ShiftDto>.Fail(
                "Shift is already closing or is not open.",
                ParkingErrorCodes.ShiftNotOpen);

        try
        {
            var summaryResult = await _paymentClient.GetShiftSummaryAsync(shift.Id, ct);
            if (!summaryResult.Success)
                return await UnlockAndFailAsync(
                    shift.Id,
                    summaryResult.Error ?? "Payment Service is unavailable.",
                    ParkingErrorCodes.PaymentServiceUnavailable);

            var summary = summaryResult.Value!;
            if (summary.PendingPaymentCount > 0)
                return await UnlockAndFailAsync(
                    shift.Id,
                    "The shift still has pending payments.",
                    ParkingErrorCodes.ShiftHasPendingPayments);

            var totals = ShiftReconciliationRules.Calculate(
                summary.CashAmount,
                summary.NonCashAmount,
                request.CountedCashAmount);
            var note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
            if (ShiftReconciliationRules.RequiresDifferenceNote(totals.DifferenceAmount) && note is null)
                return await UnlockAndFailAsync(
                    shift.Id,
                    "A note is required when counted cash differs from expected cash.",
                    ParkingErrorCodes.ShiftDifferenceNoteRequired);

            var now = DateTime.UtcNow;
            var update = Builders<Shift>.Update
                .Set(x => x.ExpectedCashAmount, totals.ExpectedCashAmount)
                .Set(x => x.TotalPayments, totals.TotalPayments)
                .Set(x => x.TotalNonCashAmount, totals.TotalNonCashAmount)
                .Set(x => x.CountedCashAmount, request.CountedCashAmount)
                .Set(x => x.DifferenceAmount, totals.DifferenceAmount)
                .Set(x => x.Note, note)
                .Set(x => x.ClosedAt, now)
                .Set(x => x.Status, ShiftStatus.Closed)
                .Set(x => x.IsClosing, false);
            var result = await _db.Shifts.UpdateOneAsync(
                x => x.Id == shift.Id && x.Status == ShiftStatus.Open && x.IsClosing,
                update,
                cancellationToken: ct);

            var closed = await _db.Shifts.Find(x => x.Id == shift.Id).FirstOrDefaultAsync(ct);
            if (result.ModifiedCount == 0 && closed?.Status != ShiftStatus.Closed)
                return await UnlockAndFailAsync(
                    shift.Id,
                    "Shift is not open.",
                    ParkingErrorCodes.ShiftNotOpen);

            return Result<ShiftDto>.Ok(Map(closed!));
        }
        catch
        {
            await UnlockAsync(shift.Id);
            throw;
        }
    }

    private async Task<Result<ShiftDto>> UnlockAndFailAsync(
        string shiftId,
        string error,
        string errorCode)
    {
        await UnlockAsync(shiftId);
        return Result<ShiftDto>.Fail(error, errorCode);
    }

    private Task UnlockAsync(string shiftId) =>
        _db.Shifts.UpdateOneAsync(
            x => x.Id == shiftId && x.Status == ShiftStatus.Open && x.IsClosing,
            Builders<Shift>.Update.Set(x => x.IsClosing, false),
            cancellationToken: CancellationToken.None);

    private static ShiftDto Map(Shift shift) => new()
    {
        Id = shift.Id,
        StaffUserId = shift.StaffUserId,
        BuildingId = shift.BuildingId,
        OpenedAt = shift.OpenedAt,
        ClosedAt = shift.ClosedAt,
        ExpectedCashAmount = shift.ExpectedCashAmount,
        TotalPayments = shift.TotalPayments,
        TotalNonCashAmount = shift.TotalNonCashAmount,
        CountedCashAmount = shift.CountedCashAmount,
        DifferenceAmount = shift.DifferenceAmount,
        Status = (int)shift.Status,
        Note = shift.Note
    };
}
