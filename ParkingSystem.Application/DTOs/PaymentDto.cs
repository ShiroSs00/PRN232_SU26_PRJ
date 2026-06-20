namespace ParkingSystem.Application.DTOs;

public class PaymentDto
{
    public string Id { get; set; } = null!;
    public string ParkingSessionId { get; set; } = string.Empty;
    public string? PlateNumber { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
