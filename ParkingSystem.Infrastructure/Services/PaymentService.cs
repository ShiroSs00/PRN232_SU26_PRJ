using MongoDB.Driver;
using ParkingSystem.Application.DTOs;
using ParkingSystem.Application.Services;
using ParkingSystem.Domain.Constants;
using ParkingSystem.Domain.Entities;
using ParkingSystem.Infrastructure.Persistence;

namespace ParkingSystem.Infrastructure.Services;

public class PaymentService : IPaymentService
{
    private readonly MongoDbContext _context;
    private readonly IParkingMapNotifier _mapNotifier;

    public PaymentService(MongoDbContext context, IParkingMapNotifier mapNotifier)
    {
        _context = context;
        _mapNotifier = mapNotifier;
    }

    public async Task<IEnumerable<PaymentDto>> GetAllAsync(string? status = null, string? method = null)
    {
        var filterBuilder = Builders<Payment>.Filter;
        var filter = filterBuilder.Empty;

        if (!string.IsNullOrEmpty(status))
        {
            filter &= filterBuilder.Eq(p => p.Status, status);
        }
        if (!string.IsNullOrEmpty(method))
        {
            filter &= filterBuilder.Eq(p => p.Method, method);
        }

        var payments = await _context.Payments.Find(filter).SortByDescending(p => p.CreatedAt).ToListAsync();

        if (!payments.Any())
        {
            return Enumerable.Empty<PaymentDto>();
        }

        var sessionIds = payments.Select(p => p.ParkingSessionId).Distinct().ToList();
        var sessions = await _context.ParkingSessions.Find(s => sessionIds.Contains(s.Id)).ToListAsync();
        var sessionMap = sessions.ToDictionary(s => s.Id);

        return payments.Select(p => MapToDto(p, sessionMap.GetValueOrDefault(p.ParkingSessionId)));
    }

    public async Task<PaymentDto?> GetByIdAsync(string id)
    {
        var payment = await _context.Payments.Find(p => p.Id == id).FirstOrDefaultAsync();
        if (payment == null)
        {
            return null;
        }

        var session = await _context.ParkingSessions.Find(s => s.Id == payment.ParkingSessionId).FirstOrDefaultAsync();
        return MapToDto(payment, session);
    }

    public async Task<PaymentDto?> GetBySessionIdAsync(string sessionId)
    {
        var payment = await _context.Payments.Find(p => p.ParkingSessionId == sessionId).FirstOrDefaultAsync();
        if (payment == null)
        {
            return null;
        }

        var session = await _context.ParkingSessions.Find(s => s.Id == payment.ParkingSessionId).FirstOrDefaultAsync();
        return MapToDto(payment, session);
    }

    public async Task<PaymentDto> CreateAsync(CreatePaymentDto dto)
    {
        var session = await _context.ParkingSessions.Find(s => s.Id == dto.ParkingSessionId).FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Parking session with ID '{dto.ParkingSessionId}' was not found.");

        var validMethods = new[] { PaymentMethods.Cash, PaymentMethods.Card, PaymentMethods.EWallet, PaymentMethods.Mock };
        if (!validMethods.Contains(dto.Method))
        {
            throw new ArgumentException($"Invalid payment method '{dto.Method}'.");
        }

        var payment = new Payment
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            ParkingSessionId = dto.ParkingSessionId,
            Amount = dto.Amount,
            Method = dto.Method,
            Status = PaymentStatuses.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Payments.InsertOneAsync(payment);

        return MapToDto(payment, session);
    }

    public async Task<PaymentDto?> ConfirmPaymentAsync(string id, ConfirmPaymentDto dto)
    {
        var payment = await _context.Payments.Find(p => p.Id == id).FirstOrDefaultAsync();
        if (payment == null)
        {
            return null;
        }

        var validMethods = new[] { PaymentMethods.Cash, PaymentMethods.Card, PaymentMethods.EWallet, PaymentMethods.Mock };
        var validStatuses = new[] { PaymentStatuses.Paid, PaymentStatuses.Failed, PaymentStatuses.Cancelled };

        if (!validMethods.Contains(dto.Method))
        {
            throw new ArgumentException($"Invalid payment method '{dto.Method}'.");
        }
        if (!validStatuses.Contains(dto.Status))
        {
            throw new ArgumentException($"Invalid payment status '{dto.Status}'.");
        }

        payment.Method = dto.Method;
        payment.Status = dto.Status;
        if (dto.Status == PaymentStatuses.Paid)
        {
            payment.PaidAt = DateTime.UtcNow;
        }
        payment.UpdatedAt = DateTime.UtcNow;

        await _context.Payments.ReplaceOneAsync(p => p.Id == payment.Id, payment);

        var session = await _context.ParkingSessions.Find(s => s.Id == payment.ParkingSessionId).FirstOrDefaultAsync();

        if (dto.Status == PaymentStatuses.Paid && session != null)
        {
            // Complete session
            session.Status = ParkingSessionStatuses.Completed;
            session.UpdatedAt = DateTime.UtcNow;
            await _context.ParkingSessions.ReplaceOneAsync(s => s.Id == session.Id, session);

            // Free the slot
            var slot = await _context.ParkingSlots.Find(s => s.Id == session.ParkingSlotId).FirstOrDefaultAsync();
            if (slot != null)
            {
                slot.Status = ParkingSlotStatuses.Available;
                slot.UpdatedAt = DateTime.UtcNow;
                await _context.ParkingSlots.ReplaceOneAsync(s => s.Id == slot.Id, slot);

                // Notify realtime map
                await _mapNotifier.NotifySlotChangedAsync(slot.FloorId, new SlotStatusChangedEvent
                {
                    FloorId = slot.FloorId,
                    SlotId = slot.Id,
                    Status = ParkingSlotStatuses.Available,
                    Vehicle = null,
                    OccurredAt = DateTime.UtcNow
                });
            }
        }

        return MapToDto(payment, session);
    }

    public async Task<PaymentDto?> RefundPaymentAsync(string id)
    {
        var payment = await _context.Payments.Find(p => p.Id == id).FirstOrDefaultAsync();
        if (payment == null)
        {
            return null;
        }

        if (payment.Status != PaymentStatuses.Paid)
        {
            throw new InvalidOperationException("Only completed payments can be refunded.");
        }

        payment.Status = PaymentStatuses.Refunded;
        payment.UpdatedAt = DateTime.UtcNow;

        await _context.Payments.ReplaceOneAsync(p => p.Id == payment.Id, payment);

        var session = await _context.ParkingSessions.Find(s => s.Id == payment.ParkingSessionId).FirstOrDefaultAsync();
        return MapToDto(payment, session);
    }

    private static PaymentDto MapToDto(Payment payment, ParkingSession? session)
    {
        return new PaymentDto
        {
            Id = payment.Id,
            ParkingSessionId = payment.ParkingSessionId,
            PlateNumber = session?.PlateNumber,
            Amount = payment.Amount,
            Method = payment.Method,
            Status = payment.Status,
            PaidAt = payment.PaidAt,
            CreatedAt = payment.CreatedAt,
            UpdatedAt = payment.UpdatedAt
        };
    }
}
