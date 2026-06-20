using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.Application.DTOs;

public class CreatePaymentDto
{
    [Required(ErrorMessage = "Parking session ID is required.")]
    public string ParkingSessionId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Amount is required.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Method is required.")]
    public string Method { get; set; } = string.Empty; // Cash, Card, EWallet, Mock
}
