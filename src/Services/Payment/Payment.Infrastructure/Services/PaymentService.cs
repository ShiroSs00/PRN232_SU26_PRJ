using MongoDB.Bson;
using MongoDB.Driver;
using Payment.Application.Abstractions;
using Payment.Application.Common;
using Payment.Application.DTOs;
using Payment.Application.DTOs.Payments;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Infrastructure.Persistence;

namespace Payment.Infrastructure.Services;

public class PaymentService : IPaymentService
{
    private readonly MongoDbContext _db;
    private readonly IShiftValidationClient _shiftValidationClient;

    public PaymentService(MongoDbContext db, IShiftValidationClient shiftValidationClient)
    {
        _db = db;
        _shiftValidationClient = shiftValidationClient;
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

    public async Task<Result<ShiftPaymentSummaryDto>> GetShiftSummaryAsync(
        string shiftId,
        CancellationToken ct = default)
    {
        var payments = await _db.Payments
            .Find(x => x.ShiftId == shiftId &&
                       (x.Status == PaymentStatus.Paid || x.Status == PaymentStatus.Pending))
            .Project(x => new { x.Amount, x.Method, x.Status })
            .ToListAsync(ct);

        return Result<ShiftPaymentSummaryDto>.Ok(new ShiftPaymentSummaryDto
        {
            ShiftId = shiftId,
            CashAmount = payments
                .Where(x => x.Status == PaymentStatus.Paid && x.Method == PaymentMethod.Cash)
                .Sum(x => x.Amount),
            NonCashAmount = payments
                .Where(x => x.Status == PaymentStatus.Paid && x.Method != PaymentMethod.Cash)
                .Sum(x => x.Amount),
            PendingPaymentCount = payments.LongCount(x => x.Status == PaymentStatus.Pending)
        });
    }

    public async Task<Result<PaymentDto>> CreateAsync(string createdByUserId, CreatePaymentRequest request, CancellationToken ct = default)
    {
        var sessionId = string.IsNullOrWhiteSpace(request.ParkingSessionId)
            ? null
            : request.ParkingSessionId.Trim();
        var subscriptionId = string.IsNullOrWhiteSpace(request.SubscriptionId)
            ? null
            : request.SubscriptionId.Trim();
        var ownerUserId = string.IsNullOrWhiteSpace(request.OwnerUserId)
            ? null
            : request.OwnerUserId.Trim();
        var shiftId = string.IsNullOrWhiteSpace(request.ShiftId)
            ? null
            : request.ShiftId.Trim();

        if ((sessionId is null) == (subscriptionId is null))
            return Result<PaymentDto>.Fail(
                "Exactly one of ParkingSessionId or SubscriptionId is required.",
                PaymentErrorCodes.ValidationFailed);
        if (string.IsNullOrWhiteSpace(request.PlateNumber))
            return Result<PaymentDto>.Fail("PlateNumber is required.", PaymentErrorCodes.ValidationFailed);
        if (request.Amount <= 0)
            return Result<PaymentDto>.Fail("Amount must be positive.", PaymentErrorCodes.ValidationFailed);
        if (!Enum.IsDefined(request.Method))
            return Result<PaymentDto>.Fail("Payment method is invalid.", PaymentErrorCodes.InvalidPaymentMethod);

        // Validate subscription status when paying for a subscription.
        if (subscriptionId is not null)
        {
            var sub = await _db.Subscriptions.Find(x => x.Id == subscriptionId).FirstOrDefaultAsync(ct);
            if (sub is null)
                return Result<PaymentDto>.Fail("Subscription not found.", PaymentErrorCodes.ValidationFailed);
            if (sub.Status != SubscriptionStatus.PendingPayment)
                return Result<PaymentDto>.Fail(
                    "Subscription is not awaiting payment.",
                    PaymentErrorCodes.ValidationFailed);
        }

        if (request.Method == PaymentMethod.Cash && shiftId is null)
            return Result<PaymentDto>.Fail(
                "Cash payments require an open shift.",
                PaymentErrorCodes.ValidationFailed);
        if (shiftId is not null &&
            !await IsCurrentShiftAsync(shiftId, createdByUserId, ct))
            return Result<PaymentDto>.Fail(
                "ShiftId must be the requesting staff member's current open shift.",
                PaymentErrorCodes.InvalidShift);

        var plate = request.PlateNumber.Trim().ToUpperInvariant();
        Domain.Entities.Payment? existing;
        if (sessionId is not null)
        {
            existing = await _db.Payments
                .Find(x => x.ParkingSessionId == sessionId &&
                           (x.Status == PaymentStatus.Pending || x.Status == PaymentStatus.Paid))
                .FirstOrDefaultAsync(ct);
        }
        else
        {
            existing = await _db.Payments
                .Find(x => x.SubscriptionId == subscriptionId &&
                           (x.Status == PaymentStatus.Pending || x.Status == PaymentStatus.Paid))
                .FirstOrDefaultAsync(ct);
        }

        if (existing is not null)
        {
            if (existing.Amount != request.Amount ||
                existing.PlateNumber != plate ||
                existing.Method != request.Method ||
                !string.Equals(existing.OwnerUserId, ownerUserId, StringComparison.Ordinal) ||
                !string.Equals(existing.ShiftId, shiftId, StringComparison.Ordinal))
                return Result<PaymentDto>.Fail(
                    "An existing payment for this source has different immutable details.",
                    PaymentErrorCodes.DuplicatePaymentForSession);

            return Result<PaymentDto>.Ok(Map(existing));
        }

        var entity = new Domain.Entities.Payment
        {
            Id = ObjectId.GenerateNewId().ToString(),
            ParkingSessionId = sessionId,
            SubscriptionId = subscriptionId,
            PlateNumber = plate,
            VehicleId = string.IsNullOrWhiteSpace(request.VehicleId) ? null : request.VehicleId.Trim(),
            ShiftId = shiftId,
            CreatedByUserId = createdByUserId,
            OwnerUserId = ownerUserId,
            Amount = request.Amount,
            Method = request.Method,
            Status = PaymentStatus.Pending,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            await _db.Payments.InsertOneAsync(entity, cancellationToken: ct);
            if (shiftId is not null &&
                !await IsCurrentShiftAsync(shiftId, createdByUserId, ct))
            {
                await _db.Payments.UpdateOneAsync(
                    x => x.Id == entity.Id && x.Status == PaymentStatus.Pending,
                    Builders<Domain.Entities.Payment>.Update.Set(x => x.Status, PaymentStatus.Cancelled),
                    cancellationToken: CancellationToken.None);
                return Result<PaymentDto>.Fail(
                    "The shift started closing while the payment was being created.",
                    PaymentErrorCodes.InvalidShift);
            }
            return Result<PaymentDto>.Ok(Map(entity));
        }
        catch (MongoWriteException ex) when (
            sessionId is not null &&
            ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            existing = await _db.Payments
                .Find(x => x.ParkingSessionId == sessionId &&
                           (x.Status == PaymentStatus.Pending || x.Status == PaymentStatus.Paid))
                .FirstOrDefaultAsync(ct);
            if (existing is not null &&
                existing.Amount == request.Amount &&
                existing.PlateNumber == plate &&
                existing.Method == request.Method &&
                string.Equals(existing.OwnerUserId, ownerUserId, StringComparison.Ordinal) &&
                string.Equals(existing.ShiftId, shiftId, StringComparison.Ordinal))
                return Result<PaymentDto>.Ok(Map(existing));

            return Result<PaymentDto>.Fail(
                "A payment already exists for this parking session.",
                PaymentErrorCodes.DuplicatePaymentForSession);
        }
    }

    private async Task<bool> IsCurrentShiftAsync(
        string shiftId,
        string staffUserId,
        CancellationToken ct)
    {
        var result = await _shiftValidationClient.GetCurrentAsync(ct);
        return result.Success &&
               result.Value?.Status == 1 &&
               string.Equals(result.Value.Id, shiftId, StringComparison.Ordinal) &&
               string.Equals(result.Value.StaffUserId, staffUserId, StringComparison.Ordinal);
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

        // Activate subscription if this payment is linked to one.
        if (!string.IsNullOrWhiteSpace(entity.SubscriptionId))
        {
            var subUpdate = Builders<Subscription>.Update
                .Set(x => x.Status, SubscriptionStatus.Active)
                .Set(x => x.UpdatedAt, now);
            await _db.Subscriptions.UpdateOneAsync(
                x => x.Id == entity.SubscriptionId && x.Status == SubscriptionStatus.PendingPayment,
                subUpdate, cancellationToken: ct);
        }

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
        OwnerUserId = x.OwnerUserId,
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
