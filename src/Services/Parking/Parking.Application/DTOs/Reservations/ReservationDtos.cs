using Parking.Domain.Enums;

namespace Parking.Application.DTOs.Reservations;

public class ReservationDto
{
    public string Id { get; set; } = string.Empty;

    public string BuildingId { get; set; } = string.Empty;

    public string VehicleTypeId { get; set; } = string.Empty;

    public string PlateNumber { get; set; } = string.Empty;

    public string? VehicleId { get; set; }

    public string? DriverUserId { get; set; }

    public string? ZoneId { get; set; }

    public string? ParkingSlotId { get; set; }

    public DateTime ReservedFrom { get; set; }

    public DateTime ReservedTo { get; set; }

    public ReservationStatus Status { get; set; }

    public string? ParkingSessionId { get; set; }

    public string? CancelledByUserId { get; set; }

    public DateTime? CancelledAt { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

public class CreateReservationRequest
{
    public string BuildingId { get; set; } = string.Empty;

    public string VehicleTypeId { get; set; } = string.Empty;

    public string PlateNumber { get; set; } = string.Empty;

    public string? VehicleId { get; set; }

    public string? ZoneId { get; set; }

    /// <summary>Optional. If provided, this exact slot is reserved (must be Available and free of conflicting reservations).</summary>
    public string? ParkingSlotId { get; set; }

    public DateTime ReservedFrom { get; set; }

    public DateTime ReservedTo { get; set; }

    public string? Note { get; set; }
}

public class UpdateReservationRequest
{
    public string? ZoneId { get; set; }

    public DateTime ReservedFrom { get; set; }

    public DateTime ReservedTo { get; set; }

    public string? Note { get; set; }
}

public class CancelReservationRequest
{
    public string? Note { get; set; }
}

public class CheckInReservationRequest
{
    public string ParkingSessionId { get; set; } = string.Empty;
}

public class ReservationListQuery
{
    public string? BuildingId { get; set; }

    public int? Status { get; set; }

    public string? PlateNumber { get; set; }

    public string? DriverUserId { get; set; }

    public DateTime? ReservedFromStart { get; set; }

    public DateTime? ReservedFromEnd { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}
