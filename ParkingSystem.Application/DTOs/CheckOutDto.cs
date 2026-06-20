using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.Application.DTOs;

public class CheckOutDto
{
    [MaxLength(50, ErrorMessage = "Exit gate cannot exceed 50 characters.")]
    public string? ExitGate { get; set; }
}
