using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.Application.DTOs;

public class CreateParkingSlotDto
{
    [Required(ErrorMessage = "Building ID is required.")]
    public string BuildingId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Floor ID is required.")]
    public string FloorId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Zone ID is required.")]
    public string ZoneId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vehicle type ID is required.")]
    public string VehicleTypeId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Code is required.")]
    [MaxLength(50, ErrorMessage = "Code cannot exceed 50 characters.")]
    public string Code { get; set; } = string.Empty;

    public int Row { get; set; }
    public int Column { get; set; }
    public double PositionX { get; set; }
    public double PositionY { get; set; }
    public double Width { get; set; } = 50;
    public double Height { get; set; } = 80;
}
