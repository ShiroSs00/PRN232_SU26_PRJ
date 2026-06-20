using MongoDB.Driver;
using ParkingSystem.Application.DTOs;
using ParkingSystem.Application.Services;
using ParkingSystem.Domain.Constants;
using ParkingSystem.Domain.Entities;
using ParkingSystem.Infrastructure.Persistence;

namespace ParkingSystem.Infrastructure.Services;

public class ParkingSessionService : IParkingSessionService
{
    private readonly MongoDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IParkingMapNotifier _mapNotifier;

    public ParkingSessionService(MongoDbContext context, ICurrentUserService currentUserService, IParkingMapNotifier mapNotifier)
    {
        _context = context;
        _currentUserService = currentUserService;
        _mapNotifier = mapNotifier;
    }

    public async Task<IEnumerable<ParkingSessionDto>> GetAllAsync(string? status = null, string? plateNumber = null)
    {
        var filterBuilder = Builders<ParkingSession>.Filter;
        var filter = filterBuilder.Empty;

        if (!string.IsNullOrEmpty(status))
        {
            filter &= filterBuilder.Eq(s => s.Status, status);
        }
        if (!string.IsNullOrEmpty(plateNumber))
        {
            filter &= filterBuilder.Regex(s => s.PlateNumber, new MongoDB.Bson.BsonRegularExpression(plateNumber, "i"));
        }

        var sessions = await _context.ParkingSessions.Find(filter).SortByDescending(s => s.CheckInTime).ToListAsync();

        if (!sessions.Any())
        {
            return Enumerable.Empty<ParkingSessionDto>();
        }

        // Fetch related entities for map
        var slotIds = sessions.Select(s => s.ParkingSlotId).Distinct().ToList();
        var vehicleTypeIds = sessions.Select(s => s.VehicleTypeId).Distinct().ToList();
        var userIds = sessions.Select(s => s.CreatedByUserId).Concat(sessions.Select(s => s.CompletedByUserId))
            .Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
        var sessionIds = sessions.Select(s => s.Id).ToList();

        var slots = await _context.ParkingSlots.Find(s => slotIds.Contains(s.Id)).ToListAsync();
        var vehicleTypes = await _context.VehicleTypes.Find(vt => vehicleTypeIds.Contains(vt.Id)).ToListAsync();
        var users = await _context.Users.Find(u => userIds.Contains(u.Id)).ToListAsync();
        var payments = await _context.Payments.Find(p => sessionIds.Contains(p.ParkingSessionId)).ToListAsync();

        var slotMap = slots.ToDictionary(s => s.Id);
        var vehicleTypeMap = vehicleTypes.ToDictionary(vt => vt.Id);
        var userMap = users.ToDictionary(u => u.Id);
        var paymentMap = payments.GroupBy(p => p.ParkingSessionId).ToDictionary(g => g.Key, g => g.First());

        return sessions.Select(s => MapToDto(
            s,
            slotMap.GetValueOrDefault(s.ParkingSlotId),
            vehicleTypeMap.GetValueOrDefault(s.VehicleTypeId),
            !string.IsNullOrEmpty(s.CreatedByUserId) ? userMap.GetValueOrDefault(s.CreatedByUserId) : null,
            !string.IsNullOrEmpty(s.CompletedByUserId) ? userMap.GetValueOrDefault(s.CompletedByUserId) : null,
            paymentMap.GetValueOrDefault(s.Id)
        ));
    }

    public async Task<ParkingSessionDto?> GetByIdAsync(string id)
    {
        var session = await _context.ParkingSessions.Find(s => s.Id == id).FirstOrDefaultAsync();
        if (session == null)
        {
            return null;
        }

        var slot = await _context.ParkingSlots.Find(s => s.Id == session.ParkingSlotId).FirstOrDefaultAsync();
        var vehicleType = await _context.VehicleTypes.Find(vt => vt.Id == session.VehicleTypeId).FirstOrDefaultAsync();
        var createdByUser = !string.IsNullOrEmpty(session.CreatedByUserId) 
            ? await _context.Users.Find(u => u.Id == session.CreatedByUserId).FirstOrDefaultAsync() 
            : null;
        var completedByUser = !string.IsNullOrEmpty(session.CompletedByUserId) 
            ? await _context.Users.Find(u => u.Id == session.CompletedByUserId).FirstOrDefaultAsync() 
            : null;
        var payment = await _context.Payments.Find(p => p.ParkingSessionId == session.Id).FirstOrDefaultAsync();

        return MapToDto(session, slot, vehicleType, createdByUser, completedByUser, payment);
    }

    public async Task<ParkingSessionDto?> GetActiveByPlateNumberAsync(string plateNumber)
    {
        var session = await _context.ParkingSessions
            .Find(s => s.PlateNumber == plateNumber && 
                      (s.Status == ParkingSessionStatuses.Active || s.Status == ParkingSessionStatuses.LostTicket))
            .SortByDescending(s => s.CheckInTime)
            .FirstOrDefaultAsync();

        if (session == null)
        {
            return null;
        }

        return await GetByIdAsync(session.Id);
    }

    public async Task<ParkingSessionDto> CheckInAsync(CheckInDto dto)
    {
        // 1. Verify vehicle type exists
        var vehicleType = await _context.VehicleTypes.Find(vt => vt.Id == dto.VehicleTypeId).FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Vehicle type with ID '{dto.VehicleTypeId}' was not found.");

        // 2. Check if vehicle is already parked (has active session)
        var isParked = await _context.ParkingSessions
            .Find(s => s.PlateNumber == dto.PlateNumber && 
                      (s.Status == ParkingSessionStatuses.Active || s.Status == ParkingSessionStatuses.LostTicket))
            .AnyAsync();

        if (isParked)
        {
            throw new InvalidOperationException($"Vehicle with plate number '{dto.PlateNumber}' is already parked in the lot.");
        }

        // 3. Find an available slot matching vehicle type
        var slot = await _context.ParkingSlots
            .Find(s => s.VehicleTypeId == dto.VehicleTypeId && s.Status == ParkingSlotStatuses.Available && s.IsActive)
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException($"No available parking slots found for vehicle type '{vehicleType.Name}'.");

        // 4. Update slot status to Occupied
        slot.Status = ParkingSlotStatuses.Occupied;
        slot.UpdatedAt = DateTime.UtcNow;
        await _context.ParkingSlots.ReplaceOneAsync(s => s.Id == slot.Id, slot);

        // 5. Create active parking session
        var currentUserId = _currentUserService.UserId;
        var session = new ParkingSession
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            PlateNumber = dto.PlateNumber,
            VehicleTypeId = dto.VehicleTypeId,
            ParkingSlotId = slot.Id,
            CheckInTime = DateTime.UtcNow,
            EntryGate = dto.EntryGate,
            Status = ParkingSessionStatuses.Active,
            CreatedByUserId = currentUserId,
            CreatedAt = DateTime.UtcNow
        };

        await _context.ParkingSessions.InsertOneAsync(session);

        // Notify realtime map
        await _mapNotifier.NotifySlotChangedAsync(slot.FloorId, new SlotStatusChangedEvent
        {
            FloorId = slot.FloorId,
            SlotId = slot.Id,
            Status = ParkingSlotStatuses.Occupied,
            Vehicle = new OccupyingVehicleDto
            {
                SessionId = session.Id,
                PlateNumber = session.PlateNumber,
                CheckInTime = session.CheckInTime,
                IsMonthly = false
            },
            OccurredAt = DateTime.UtcNow
        });

        var createdByUser = !string.IsNullOrEmpty(currentUserId) 
            ? await _context.Users.Find(u => u.Id == currentUserId).FirstOrDefaultAsync() 
            : null;

        return MapToDto(session, slot, vehicleType, createdByUser, null, null);
    }

    public async Task<ParkingSessionDto?> CheckOutAsync(string id, CheckOutDto dto)
    {
        var session = await _context.ParkingSessions.Find(s => s.Id == id).FirstOrDefaultAsync();
        if (session == null)
        {
            return null;
        }

        if (session.Status != ParkingSessionStatuses.Active && session.Status != ParkingSessionStatuses.LostTicket)
        {
            throw new InvalidOperationException("This parking session is not active and cannot be checked out.");
        }

        var checkOutTime = DateTime.UtcNow;
        var fee = await CalculateSessionFeeAsync(session, checkOutTime);

        // Update session check-out info (retains status until payment confirmed)
        var currentUserId = _currentUserService.UserId;
        session.CheckOutTime = checkOutTime;
        session.ExitGate = dto.ExitGate;
        session.TotalFee = fee;
        session.CompletedByUserId = currentUserId;
        session.UpdatedAt = DateTime.UtcNow;

        await _context.ParkingSessions.ReplaceOneAsync(s => s.Id == session.Id, session);

        // Create or update Payment
        var payment = await _context.Payments.Find(p => p.ParkingSessionId == session.Id).FirstOrDefaultAsync();
        if (payment == null)
        {
            payment = new Payment
            {
                Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                ParkingSessionId = session.Id,
                Amount = fee,
                Method = PaymentMethods.Mock,
                Status = PaymentStatuses.Pending,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Payments.InsertOneAsync(payment);
        }
        else
        {
            payment.Amount = fee;
            payment.UpdatedAt = DateTime.UtcNow;
            await _context.Payments.ReplaceOneAsync(p => p.Id == payment.Id, payment);
        }

        var slot = await _context.ParkingSlots.Find(s => s.Id == session.ParkingSlotId).FirstOrDefaultAsync();
        var vehicleType = await _context.VehicleTypes.Find(vt => vt.Id == session.VehicleTypeId).FirstOrDefaultAsync();
        var createdByUser = !string.IsNullOrEmpty(session.CreatedByUserId) ? await _context.Users.Find(u => u.Id == session.CreatedByUserId).FirstOrDefaultAsync() : null;
        var completedByUser = !string.IsNullOrEmpty(currentUserId) ? await _context.Users.Find(u => u.Id == currentUserId).FirstOrDefaultAsync() : null;

        return MapToDto(session, slot, vehicleType, createdByUser, completedByUser, payment);
    }

    public async Task<ParkingSessionDto?> ConfirmPaymentAsync(string id, ConfirmPaymentDto dto)
    {
        var session = await _context.ParkingSessions.Find(s => s.Id == id).FirstOrDefaultAsync();
        if (session == null)
        {
            return null;
        }

        var payment = await _context.Payments.Find(p => p.ParkingSessionId == session.Id).FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("No payment records found for this parking session. Please check out first.");

        // Validate payment status/method parameters
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

        var slot = await _context.ParkingSlots.Find(s => s.Id == session.ParkingSlotId).FirstOrDefaultAsync();

        if (dto.Status == PaymentStatuses.Paid)
        {
            // Complete session
            session.Status = ParkingSessionStatuses.Completed;
            session.UpdatedAt = DateTime.UtcNow;
            await _context.ParkingSessions.ReplaceOneAsync(s => s.Id == session.Id, session);

            // Free the slot
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

        var vehicleType = await _context.VehicleTypes.Find(vt => vt.Id == session.VehicleTypeId).FirstOrDefaultAsync();
        var createdByUser = !string.IsNullOrEmpty(session.CreatedByUserId) ? await _context.Users.Find(u => u.Id == session.CreatedByUserId).FirstOrDefaultAsync() : null;
        var completedByUser = !string.IsNullOrEmpty(session.CompletedByUserId) ? await _context.Users.Find(u => u.Id == session.CompletedByUserId).FirstOrDefaultAsync() : null;

        return MapToDto(session, slot, vehicleType, createdByUser, completedByUser, payment);
    }

    public async Task<ParkingSessionDto?> MarkLostTicketAsync(string id)
    {
        var session = await _context.ParkingSessions.Find(s => s.Id == id).FirstOrDefaultAsync();
        if (session == null)
        {
            return null;
        }

        if (session.Status != ParkingSessionStatuses.Active)
        {
            throw new InvalidOperationException("Only active parking sessions can be marked as lost ticket.");
        }

        session.Status = ParkingSessionStatuses.LostTicket;
        session.UpdatedAt = DateTime.UtcNow;
        await _context.ParkingSessions.ReplaceOneAsync(s => s.Id == session.Id, session);

        return await GetByIdAsync(session.Id);
    }

    public async Task<ParkingSessionDto?> CancelAsync(string id)
    {
        var session = await _context.ParkingSessions.Find(s => s.Id == id).FirstOrDefaultAsync();
        if (session == null)
        {
            return null;
        }

        if (session.Status != ParkingSessionStatuses.Active && session.Status != ParkingSessionStatuses.LostTicket)
        {
            throw new InvalidOperationException("Only active or lost ticket parking sessions can be cancelled.");
        }

        session.Status = ParkingSessionStatuses.Cancelled;
        session.UpdatedAt = DateTime.UtcNow;
        await _context.ParkingSessions.ReplaceOneAsync(s => s.Id == session.Id, session);

        // Free slot
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

        return await GetByIdAsync(session.Id);
    }

    private async Task<decimal> CalculateSessionFeeAsync(ParkingSession session, DateTime checkOutTime)
    {
        var policy = await _context.FeePolicies
            .Find(p => p.VehicleTypeId == session.VehicleTypeId && p.IsActive)
            .SortByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

        var duration = checkOutTime - session.CheckInTime;
        var totalMinutes = duration.TotalMinutes;

        // Fallbacks if no policy is registered
        decimal basePrice = policy?.BasePrice ?? (session.VehicleTypeId.Contains("car") ? 20000m : 5000m);
        decimal hourlyPrice = policy?.HourlyPrice ?? (session.VehicleTypeId.Contains("car") ? 10000m : 3000m);
        decimal dailyPrice = policy?.DailyPrice ?? (session.VehicleTypeId.Contains("car") ? 150000m : 30000m);
        decimal lostTicketFee = policy?.LostTicketFee ?? 100000m;

        decimal totalFee = basePrice;
        var pricingType = policy?.PricingType ?? PricingTypes.Hourly;

        if (pricingType == PricingTypes.Hourly)
        {
            if (totalMinutes > 60)
            {
                var extraMinutes = totalMinutes - 60;
                var extraHours = (int)Math.Ceiling(extraMinutes / 60.0);
                totalFee += extraHours * hourlyPrice;
            }
        }
        else if (pricingType == PricingTypes.Daily)
        {
            if (totalMinutes > 1440)
            {
                var extraDays = (int)Math.Ceiling((totalMinutes - 1440) / 1440.0);
                totalFee += extraDays * dailyPrice;
            }
        }

        // Add lost ticket penalty if applicable
        if (session.Status == ParkingSessionStatuses.LostTicket)
        {
            totalFee += lostTicketFee;
        }

        return totalFee;
    }

    private static ParkingSessionDto MapToDto(
        ParkingSession session,
        ParkingSlot? slot,
        VehicleType? vehicleType,
        User? createdByUser,
        User? completedByUser,
        Payment? payment)
    {
        return new ParkingSessionDto
        {
            Id = session.Id,
            PlateNumber = session.PlateNumber,
            VehicleTypeId = session.VehicleTypeId,
            VehicleTypeName = vehicleType?.Name,
            ParkingSlotId = session.ParkingSlotId,
            ParkingSlotCode = slot?.Code,
            CheckInTime = session.CheckInTime,
            CheckOutTime = session.CheckOutTime,
            EntryGate = session.EntryGate,
            ExitGate = session.ExitGate,
            Status = session.Status,
            TotalFee = session.TotalFee,
            CreatedByUserId = session.CreatedByUserId,
            CreatedByUserName = createdByUser?.FullName,
            CompletedByUserId = session.CompletedByUserId,
            CompletedByUserName = completedByUser?.FullName,
            PaymentId = payment?.Id,
            PaymentAmount = payment?.Amount,
            PaymentMethod = payment?.Method,
            PaymentStatus = payment?.Status,
            PaidAt = payment?.PaidAt,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt
        };
    }
}
