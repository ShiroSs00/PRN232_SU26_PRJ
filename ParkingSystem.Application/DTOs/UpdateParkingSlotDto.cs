using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.Application.DTOs;

public class UpdateParkingSlotDto
{
    [Required(ErrorMessage = "Code is required.")]
    [MaxLength(50, ErrorMessage = "Code cannot exceed 50 characters.")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vehicle type ID is required.")]
    public string VehicleTypeId { get; set; } = string.Empty;

    public int Row { get; set; }
    public int Column { get; set; }
    public double PositionX { get; set; }
    public double PositionY { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}
