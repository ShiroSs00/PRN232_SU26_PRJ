namespace ParkingSystem.Application.DTOs;

public class ParkingSessionDto
{
    public string Id { get; set; } = null!;
    public string PlateNumber { get; set; } = string.Empty;
    public string VehicleTypeId { get; set; } = string.Empty;
    public string? VehicleTypeName { get; set; }
    public string ParkingSlotId { get; set; } = string.Empty;
    public string? ParkingSlotCode { get; set; }
    public DateTime CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public string? EntryGate { get; set; }
    public string? ExitGate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal? TotalFee { get; set; }
    public string? CreatedByUserId { get; set; }
    public string? CreatedByUserName { get; set; }
    public string? CompletedByUserId { get; set; }
    public string? CompletedByUserName { get; set; }
    
    // Linked payment details
    public string? PaymentId { get; set; }
    public decimal? PaymentAmount { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PaymentStatus { get; set; }
    public DateTime? PaidAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
