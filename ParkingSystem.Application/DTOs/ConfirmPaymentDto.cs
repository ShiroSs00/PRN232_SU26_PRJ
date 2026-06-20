using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.Application.DTOs;

public class ConfirmPaymentDto
{
    [Required(ErrorMessage = "Payment method is required.")]
    public string Method { get; set; } = string.Empty; // Cash, Card, EWallet, Mock

    [Required(ErrorMessage = "Payment status is required.")]
    public string Status { get; set; } = string.Empty; // Paid, Failed
}
