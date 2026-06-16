using Parking.Domain.Enums;
using Shared.Common.Entities;

namespace Parking.Domain.Entities;

public class Reservation : AuditableEntity
{
    public string BuildingId { get; set; } = string.Empty;

    public string VehicleTypeId { get; set; } = string.Empty;

    public string PlateNumber { get; set; } = string.Empty;

    public string? VehicleId { get; set; }

    public string? DriverUserId { get; set; }
    public string? ZoneId { get; set; }

    public string? ParkingSlotId { get; set; }

    public DateTime ReservedFrom { get; set; }

    public DateTime ReservedTo { get; set; }

    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

    public string? ParkingSessionId { get; set; }

    public string? CancelledByUserId { get; set; }

    public DateTime? CancelledAt { get; set; }

    public string? Note { get; set; }
}
