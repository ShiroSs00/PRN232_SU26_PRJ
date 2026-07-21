using MongoDB.Bson;
using MongoDB.Driver;
using Payment.Application.Abstractions;
using Payment.Application.Common;
using Payment.Application.DTOs;
using Payment.Application.DTOs.Subscriptions;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Infrastructure.Persistence;

namespace Payment.Infrastructure.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly MongoDbContext _db;

    public SubscriptionService(MongoDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PagedResult<SubscriptionDto>>> GetListAsync(
        int? status,
        string? buildingId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 200) pageSize = 200;

        var fb = Builders<Subscription>.Filter;
        var filters = new List<FilterDefinition<Subscription>>();
        if (status.HasValue) filters.Add(fb.Eq(x => x.Status, (SubscriptionStatus)status.Value));
        if (!string.IsNullOrWhiteSpace(buildingId)) filters.Add(fb.Eq(x => x.BuildingId, buildingId));

        var filter = filters.Count == 0 ? fb.Empty : fb.And(filters);
        var total = await _db.Subscriptions.CountDocumentsAsync(filter, cancellationToken: ct);

        var items = await _db.Subscriptions.Find(filter)
            .SortByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(ct);

        return Result<PagedResult<SubscriptionDto>>.Ok(new PagedResult<SubscriptionDto>
        {
            Items = items.Select(Map).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        });
    }

    public async Task<Result<SubscriptionDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var entity = await _db.Subscriptions.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (entity is null)
            return Result<SubscriptionDto>.Fail("Subscription not found.", PaymentErrorCodes.SubscriptionNotFound);
        return Result<SubscriptionDto>.Ok(Map(entity));
    }

    public async Task<Result<SubscriptionDto?>> GetActiveByPlateAsync(string plateNumber, CancellationToken ct = default)
    {
        var plate = plateNumber.Trim().ToUpperInvariant();
        var now = DateTime.UtcNow;

        var entity = await _db.Subscriptions
            .Find(x => x.PlateNumber == plate &&
                       x.Status == SubscriptionStatus.Active &&
                       x.StartDate <= now &&
                       x.EndDate >= now)
            .SortByDescending(x => x.EndDate)
            .FirstOrDefaultAsync(ct);

        return Result<SubscriptionDto?>.Ok(entity is null ? null : Map(entity));
    }

    public async Task<Result<SubscriptionDto>> CreateAsync(CreateSubscriptionRequest request, CancellationToken ct = default)
    {
        var validation = Validate(request.PlateNumber, request.VehicleTypeId, request.BuildingId, request.OwnerName,
            request.OwnerPhone, request.MonthlyFee, request.StartDate, request.EndDate);
        if (validation is not null)
            return Result<SubscriptionDto>.Fail(validation, PaymentErrorCodes.ValidationFailed);

        var entity = new Subscription
        {
            Id = ObjectId.GenerateNewId().ToString(),
            PlateNumber = request.PlateNumber.Trim().ToUpperInvariant(),
            VehicleId = string.IsNullOrWhiteSpace(request.VehicleId) ? null : request.VehicleId,
            VehicleTypeId = request.VehicleTypeId,
            BuildingId = request.BuildingId,
            OwnerName = request.OwnerName.Trim(),
            OwnerPhone = request.OwnerPhone.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            MonthlyFee = request.MonthlyFee,
            Status = SubscriptionStatus.Active,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _db.Subscriptions.InsertOneAsync(entity, cancellationToken: ct);
        return Result<SubscriptionDto>.Ok(Map(entity));
    }

    public async Task<Result<SubscriptionDto>> UpdateAsync(string id, UpdateSubscriptionRequest request, CancellationToken ct = default)
    {
        var entity = await _db.Subscriptions.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (entity is null)
            return Result<SubscriptionDto>.Fail("Subscription not found.", PaymentErrorCodes.SubscriptionNotFound);

        var validation = Validate(entity.PlateNumber, request.VehicleTypeId, request.BuildingId, request.OwnerName,
            request.OwnerPhone, request.MonthlyFee, request.StartDate, request.EndDate);
        if (validation is not null)
            return Result<SubscriptionDto>.Fail(validation, PaymentErrorCodes.ValidationFailed);

        var update = Builders<Subscription>.Update
            .Set(x => x.VehicleId, string.IsNullOrWhiteSpace(request.VehicleId) ? null : request.VehicleId)
            .Set(x => x.VehicleTypeId, request.VehicleTypeId)
            .Set(x => x.BuildingId, request.BuildingId)
            .Set(x => x.OwnerName, request.OwnerName.Trim())
            .Set(x => x.OwnerPhone, request.OwnerPhone.Trim())
            .Set(x => x.StartDate, request.StartDate)
            .Set(x => x.EndDate, request.EndDate)
            .Set(x => x.MonthlyFee, request.MonthlyFee)
            .Set(x => x.Note, string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim())
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        await _db.Subscriptions.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);
        var updated = await _db.Subscriptions.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        return Result<SubscriptionDto>.Ok(Map(updated!));
    }

    public async Task<Result<SubscriptionDto>> RenewAsync(string id, RenewSubscriptionRequest request, CancellationToken ct = default)
    {
        var entity = await _db.Subscriptions.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (entity is null)
            return Result<SubscriptionDto>.Fail("Subscription not found.", PaymentErrorCodes.SubscriptionNotFound);
        if (request.Months <= 0)
            return Result<SubscriptionDto>.Fail("Months must be a positive number.", PaymentErrorCodes.ValidationFailed);

        var newEnd = (entity.EndDate >= DateTime.UtcNow ? entity.EndDate : DateTime.UtcNow).AddMonths(request.Months);
        var update = Builders<Subscription>.Update
            .Set(x => x.EndDate, newEnd)
            .Set(x => x.Status, SubscriptionStatus.Active)
            .Set(x => x.SuspendedAt, (DateTime?)null)
            .Set(x => x.CancelledAt, (DateTime?)null)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        await _db.Subscriptions.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);
        var updated = await _db.Subscriptions.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        return Result<SubscriptionDto>.Ok(Map(updated!));
    }

    public async Task<Result<SubscriptionDto>> SuspendAsync(string id, CancellationToken ct = default)
    {
        var entity = await _db.Subscriptions.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (entity is null)
            return Result<SubscriptionDto>.Fail("Subscription not found.", PaymentErrorCodes.SubscriptionNotFound);

        var now = DateTime.UtcNow;
        var update = Builders<Subscription>.Update
            .Set(x => x.Status, SubscriptionStatus.Suspended)
            .Set(x => x.SuspendedAt, now)
            .Set(x => x.UpdatedAt, now);

        await _db.Subscriptions.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);
        var updated = await _db.Subscriptions.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        return Result<SubscriptionDto>.Ok(Map(updated!));
    }

    public async Task<Result<SubscriptionDto>> CancelAsync(string id, CancellationToken ct = default)
    {
        var entity = await _db.Subscriptions.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (entity is null)
            return Result<SubscriptionDto>.Fail("Subscription not found.", PaymentErrorCodes.SubscriptionNotFound);

        var now = DateTime.UtcNow;
        var update = Builders<Subscription>.Update
            .Set(x => x.Status, SubscriptionStatus.Cancelled)
            .Set(x => x.CancelledAt, now)
            .Set(x => x.UpdatedAt, now);

        await _db.Subscriptions.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);
        var updated = await _db.Subscriptions.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        return Result<SubscriptionDto>.Ok(Map(updated!));
    }

    private static string? Validate(
        string plateNumber, string vehicleTypeId, string buildingId, string ownerName,
        string ownerPhone, decimal monthlyFee, DateTime startDate, DateTime endDate)
    {
        if (string.IsNullOrWhiteSpace(plateNumber)) return "PlateNumber is required.";
        if (string.IsNullOrWhiteSpace(vehicleTypeId)) return "VehicleTypeId is required.";
        if (string.IsNullOrWhiteSpace(buildingId)) return "BuildingId is required.";
        if (string.IsNullOrWhiteSpace(ownerName)) return "OwnerName is required.";
        if (string.IsNullOrWhiteSpace(ownerPhone)) return "OwnerPhone is required.";
        if (monthlyFee < 0) return "MonthlyFee must be non-negative.";
        if (endDate <= startDate) return "EndDate must be after StartDate.";
        return null;
    }

    public async Task<Result<PagedResult<SubscriptionDto>>> GetMySubscriptionsAsync(
        string userId,
        int? status,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 200) pageSize = 200;

        var fb = Builders<Subscription>.Filter;
        var filters = new List<FilterDefinition<Subscription>>
        {
            fb.Eq(x => x.CreatedByUserId, userId)
        };
        if (status.HasValue) filters.Add(fb.Eq(x => x.Status, (SubscriptionStatus)status.Value));

        var filter = fb.And(filters);
        var total = await _db.Subscriptions.CountDocumentsAsync(filter, cancellationToken: ct);

        var items = await _db.Subscriptions.Find(filter)
            .SortByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(ct);

        return Result<PagedResult<SubscriptionDto>>.Ok(new PagedResult<SubscriptionDto>
        {
            Items = items.Select(Map).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        });
    }

    public async Task<Result<SubscriptionDto>> RequestAsync(
        CreateSubscriptionRequest request,
        string userId,
        CancellationToken ct = default)
    {
        var validation = Validate(request.PlateNumber, request.VehicleTypeId, request.BuildingId, request.OwnerName,
            request.OwnerPhone, request.MonthlyFee, request.StartDate, request.EndDate);
        if (validation is not null)
            return Result<SubscriptionDto>.Fail(validation, PaymentErrorCodes.ValidationFailed);

        var entity = new Subscription
        {
            Id = ObjectId.GenerateNewId().ToString(),
            PlateNumber = request.PlateNumber.Trim().ToUpperInvariant(),
            VehicleId = string.IsNullOrWhiteSpace(request.VehicleId) ? null : request.VehicleId,
            VehicleTypeId = request.VehicleTypeId,
            BuildingId = request.BuildingId,
            OwnerName = request.OwnerName.Trim(),
            OwnerPhone = request.OwnerPhone.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            MonthlyFee = request.MonthlyFee,
            Status = SubscriptionStatus.PendingApproval,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _db.Subscriptions.InsertOneAsync(entity, cancellationToken: ct);
        return Result<SubscriptionDto>.Ok(Map(entity));
    }

    public async Task<Result<SubscriptionDto>> ApproveAsync(
        string id,
        string userId,
        CancellationToken ct = default)
    {
        var entity = await _db.Subscriptions.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (entity is null)
            return Result<SubscriptionDto>.Fail("Subscription not found.", PaymentErrorCodes.SubscriptionNotFound);
        if (entity.Status != SubscriptionStatus.PendingApproval)
            return Result<SubscriptionDto>.Fail("Only pending subscriptions can be approved.", PaymentErrorCodes.ValidationFailed);

        var now = DateTime.UtcNow;
        var update = Builders<Subscription>.Update
            .Set(x => x.Status, SubscriptionStatus.Active)
            .Set(x => x.ApprovedByUserId, userId)
            .Set(x => x.ApprovedAt, now)
            .Set(x => x.RejectionReason, (string?)null)
            .Set(x => x.UpdatedAt, now);

        await _db.Subscriptions.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);
        var updated = await _db.Subscriptions.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        return Result<SubscriptionDto>.Ok(Map(updated!));
    }

    public async Task<Result<SubscriptionDto>> RejectAsync(
        string id,
        string? reason,
        CancellationToken ct = default)
    {
        var entity = await _db.Subscriptions.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (entity is null)
            return Result<SubscriptionDto>.Fail("Subscription not found.", PaymentErrorCodes.SubscriptionNotFound);
        if (entity.Status != SubscriptionStatus.PendingApproval)
            return Result<SubscriptionDto>.Fail("Only pending subscriptions can be rejected.", PaymentErrorCodes.ValidationFailed);

        var now = DateTime.UtcNow;
        var update = Builders<Subscription>.Update
            .Set(x => x.Status, SubscriptionStatus.Cancelled)
            .Set(x => x.CancelledAt, now)
            .Set(x => x.RejectionReason, string.IsNullOrWhiteSpace(reason) ? null : reason.Trim())
            .Set(x => x.UpdatedAt, now);

        await _db.Subscriptions.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);
        var updated = await _db.Subscriptions.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        return Result<SubscriptionDto>.Ok(Map(updated!));
    }

    private static SubscriptionDto Map(Subscription x) => new()
    {
        Id = x.Id,
        PlateNumber = x.PlateNumber,
        VehicleId = x.VehicleId,
        VehicleTypeId = x.VehicleTypeId,
        BuildingId = x.BuildingId,
        OwnerName = x.OwnerName,
        OwnerPhone = x.OwnerPhone,
        StartDate = x.StartDate,
        EndDate = x.EndDate,
        MonthlyFee = x.MonthlyFee,
        Status = x.Status,
        SuspendedAt = x.SuspendedAt,
        CancelledAt = x.CancelledAt,
        Note = x.Note,
        CreatedByUserId = x.CreatedByUserId,
        ApprovedByUserId = x.ApprovedByUserId,
        ApprovedAt = x.ApprovedAt,
        RejectionReason = x.RejectionReason,
        CreatedAt = x.CreatedAt,
        UpdatedAt = x.UpdatedAt
    };
}
