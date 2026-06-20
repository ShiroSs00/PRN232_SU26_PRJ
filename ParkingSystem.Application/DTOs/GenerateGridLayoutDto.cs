using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.Application.DTOs;

public class GenerateGridLayoutDto
{
    [Required(ErrorMessage = "GridRows is required.")]
    [Range(1, 100, ErrorMessage = "GridRows must be between 1 and 100.")]
    public int GridRows { get; set; }

    [Required(ErrorMessage = "GridCols is required.")]
    [Range(1, 100, ErrorMessage = "GridCols must be between 1 and 100.")]
    public int GridCols { get; set; }

    [Required(ErrorMessage = "Zone ID is required.")]
    public string ZoneId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vehicle type ID is required.")]
    public string VehicleTypeId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Code prefix is required.")]
    [MaxLength(20, ErrorMessage = "Code prefix cannot exceed 20 characters.")]
    public string Prefix { get; set; } = string.Empty;
}
