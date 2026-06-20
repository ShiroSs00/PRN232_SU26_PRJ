using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.Application.DTOs;

public class CheckInDto
{
    [Required(ErrorMessage = "Plate number is required.")]
    [MaxLength(20, ErrorMessage = "Plate number cannot exceed 20 characters.")]
    public string PlateNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vehicle type ID is required.")]
    public string VehicleTypeId { get; set; } = string.Empty;

    [MaxLength(50, ErrorMessage = "Entry gate cannot exceed 50 characters.")]
    public string? EntryGate { get; set; }
}
