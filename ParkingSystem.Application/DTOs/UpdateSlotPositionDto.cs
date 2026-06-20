using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.Application.DTOs;

public class UpdateSlotPositionDto
{
    [Required(ErrorMessage = "Row is required.")]
    [Range(0, 1000, ErrorMessage = "Row must be between 0 and 1000.")]
    public int Row { get; set; }

    [Required(ErrorMessage = "Column is required.")]
    [Range(0, 1000, ErrorMessage = "Column must be between 0 and 1000.")]
    public int Column { get; set; }

    [Range(1, 100, ErrorMessage = "RowSpan must be at least 1.")]
    public int RowSpan { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "ColSpan must be at least 1.")]
    public int ColSpan { get; set; } = 1;

    public string? Label { get; set; }
}
