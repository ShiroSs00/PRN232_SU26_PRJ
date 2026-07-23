using Parking.Domain.Enums;
using Shared.Common.Entities;

namespace Parking.Domain.Entities;

public class ParkingSession : BaseEntity
{
    public string PlateNumber { get; set; } = string.Empty;

    public string VehicleTypeId { get; set; } = string.Empty;

    public string? VehicleId { get; set; }

    public string BuildingId { get; set; } = string.Empty;

    public string ZoneId { get; set; } = string.Empty;

    public string ParkingSlotId { get; set; } = string.Empty;

    public string? ShiftId { get; set; }

    public string? PaymentId { get; set; }

    public string? ReservationId { get; set; }

    public DateTime CheckInTime { get; set; } = DateTime.UtcNow;

    public DateTime? CheckOutTime { get; set; }

    public string? EntryGate { get; set; }

    public string? ExitGate { get; set; }

    public string? CheckInNote { get; set; }

    public string? CheckOutNote { get; set; }

    public ParkingSessionStatus Status { get; set; } = ParkingSessionStatus.Active;

    public bool IsMonthly { get; set; }

    public string? SubscriptionId { get; set; }

    public bool IsLostTicket { get; set; }

    public decimal TotalFee { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;

    public string? CompletedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
