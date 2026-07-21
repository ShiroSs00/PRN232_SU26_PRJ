using MongoDB.Bson;
using MongoDB.Driver;
using Payment.Application.Abstractions;
using Payment.Application.Common;
using Payment.Application.DTOs;
using Payment.Application.DTOs.Payments;
using Payment.Domain.Enums;
using Payment.Infrastructure.Persistence;

namespace Payment.Infrastructure.Services;

public class PaymentService : IPaymentService
{
    private readonly MongoDbContext _db;

    public PaymentService(MongoDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PagedResult<PaymentDto>>> GetListAsync(
        string? sessionId,
        string? plateNumber,
        int? status,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 200) pageSize = 200;

        var fb = Builders<Domain.Entities.Payment>.Filter;
        var filters = new List<FilterDefinition<Domain.Entities.Payment>>();
        if (!string.IsNullOrWhiteSpace(sessionId)) filters.Add(fb.Eq(x => x.ParkingSessionId, sessionId));
        if (!string.IsNullOrWhiteSpace(plateNumber)) filters.Add(fb.Eq(x => x.PlateNumber, plateNumber));
        if (status.HasValue) filters.Add(fb.Eq(x => x.Status, (PaymentStatus)status.Value));

        var filter = filters.Count == 0 ? fb.Empty : fb.And(filters);
        var total = await _db.Payments.CountDocumentsAsync(filter, cancellationToken: ct);

        var items = await _db.Payments.Find(filter)
            .SortByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(ct);

        return Result<PagedResult<PaymentDto>>.Ok(new PagedResult<PaymentDto>
        {
            Items = items.Select(Map).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        });
    }

    public async Task<Result<PaymentDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var entity = await _db.Payments.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (entity is null)
            return Result<PaymentDto>.Fail("Payment not found.", PaymentErrorCodes.PaymentNotFound);
        return Result<PaymentDto>.Ok(Map(entity));
    }

    public async Task<Result<List<PaymentDto>>> GetBySessionAsync(string sessionId, CancellationToken ct = default)
    {
        var items = await _db.Payments.Find(x => x.ParkingSessionId == sessionId)
            .SortByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
        return Result<List<PaymentDto>>.Ok(items.Select(Map).ToList());
    }

    public async Task<Result<List<PaymentDto>>> GetBySubscriptionAsync(string subscriptionId, CancellationToken ct = default)
    {
        var items = await _db.Payments.Find(x => x.SubscriptionId == subscriptionId)
            .SortByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
        return Result<List<PaymentDto>>.Ok(items.Select(Map).ToList());
    }

    public async Task<Result<PaymentDto>> CreateAsync(string createdByUserId, CreatePaymentRequest request, CancellationToken ct = default)
    {
        var hasSession = !string.IsNullOrWhiteSpace(request.ParkingSessionId);
        var hasSubscription = !string.IsNullOrWhiteSpace(request.SubscriptionId);

        if (!hasSession && !hasSubscription)
            return Result<PaymentDto>.Fail("ParkingSessionId or SubscriptionId is required.", PaymentErrorCodes.ValidationFailed);
        if (string.IsNullOrWhiteSpace(request.PlateNumber))
            return Result<PaymentDto>.Fail("PlateNumber is required.", PaymentErrorCodes.ValidationFailed);
        if (request.Amount <= 0)
            return Result<PaymentDto>.Fail("Amount must be positive.", PaymentErrorCodes.ValidationFailed);

        // Disallow duplicate Pending/Paid payment for the same session or subscription.
        if (hasSession)
        {
            var existing = await _db.Payments
                .Find(x => x.ParkingSessionId == request.ParkingSessionId &&
                           (x.Status == PaymentStatus.Pending || x.Status == PaymentStatus.Paid))
                .FirstOrDefaultAsync(ct);
            if (existing is not null)
                return Result<PaymentDto>.Fail(
                    $"A {existing.Status} payment already exists for this session.",
                    PaymentErrorCodes.DuplicatePaymentForSession);
        }

        if (hasSubscription)
        {
            var existing = await _db.Payments
                .Find(x => x.SubscriptionId == request.SubscriptionId &&
                           (x.Status == PaymentStatus.Pending || x.Status == PaymentStatus.Paid))
                .FirstOrDefaultAsync(ct);
            if (existing is not null)
                return Result<PaymentDto>.Fail(
                    $"A {existing.Status} payment already exists for this subscription.",
                    PaymentErrorCodes.DuplicatePaymentForSession);
        }

        var entity = new Domain.Entities.Payment
        {
            Id = ObjectId.GenerateNewId().ToString(),
            ParkingSessionId = hasSession ? request.ParkingSessionId!.Trim() : null,
            SubscriptionId = hasSubscription ? request.SubscriptionId!.Trim() : null,
            PlateNumber = request.PlateNumber.Trim().ToUpperInvariant(),
            VehicleId = string.IsNullOrWhiteSpace(request.VehicleId) ? null : request.VehicleId,
            ShiftId = string.IsNullOrWhiteSpace(request.ShiftId) ? null : request.ShiftId,
            CreatedByUserId = createdByUserId,
            Amount = request.Amount,
            Method = request.Method,
            Status = PaymentStatus.Pending,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _db.Payments.InsertOneAsync(entity, cancellationToken: ct);
        return Result<PaymentDto>.Ok(Map(entity));
    }

    public async Task<Result<PaymentDto>> ConfirmAsync(string id, string confirmedByUserId, CancellationToken ct = default)
    {
        var entity = await _db.Payments.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (entity is null)
            return Result<PaymentDto>.Fail("Payment not found.", PaymentErrorCodes.PaymentNotFound);
        if (entity.Status != PaymentStatus.Pending)
            return Result<PaymentDto>.Fail(
                $"Only Pending payments can be confirmed (current: {entity.Status}).",
                PaymentErrorCodes.InvalidStatusTransition);

        var now = DateTime.UtcNow;
        var update = Builders<Domain.Entities.Payment>.Update
            .Set(x => x.Status, PaymentStatus.Paid)
            .Set(x => x.PaidAt, now)
            .Set(x => x.ConfirmedByUserId, confirmedByUserId);
        await _db.Payments.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);

        var updated = await _db.Payments.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        return Result<PaymentDto>.Ok(Map(updated!));
    }

    public async Task<Result<PaymentDto>> CancelAsync(string id, CancellationToken ct = default)
    {
        var entity = await _db.Payments.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (entity is null)
            return Result<PaymentDto>.Fail("Payment not found.", PaymentErrorCodes.PaymentNotFound);
        if (entity.Status != PaymentStatus.Pending)
            return Result<PaymentDto>.Fail(
                $"Only Pending payments can be cancelled (current: {entity.Status}).",
                PaymentErrorCodes.InvalidStatusTransition);

        var update = Builders<Domain.Entities.Payment>.Update
            .Set(x => x.Status, PaymentStatus.Cancelled);
        await _db.Payments.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);

        var updated = await _db.Payments.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        return Result<PaymentDto>.Ok(Map(updated!));
    }

    private static PaymentDto Map(Domain.Entities.Payment x) => new()
    {
        Id = x.Id,
        ParkingSessionId = x.ParkingSessionId,
        SubscriptionId = x.SubscriptionId,
        PlateNumber = x.PlateNumber,
        VehicleId = x.VehicleId,
        ShiftId = x.ShiftId,
        CreatedByUserId = x.CreatedByUserId,
        ConfirmedByUserId = x.ConfirmedByUserId,
        TransactionCode = x.TransactionCode,
        Amount = x.Amount,
        Method = x.Method,
        Status = x.Status,
        OrderCode = x.OrderCode,
        PaymentLinkId = x.PaymentLinkId,
        CheckoutUrl = x.CheckoutUrl,
        CreatedAt = x.CreatedAt,
        PaidAt = x.PaidAt,
        RefundedAt = x.RefundedAt,
        Note = x.Note
    };
}
