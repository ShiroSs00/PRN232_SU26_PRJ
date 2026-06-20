using System.ComponentModel.DataAnnotations;

namespace ParkingSystem.Application.DTOs;

public class UpdateSlotStatusDto
{
    [Required(ErrorMessage = "Status is required.")]
    public string Status { get; set; } = string.Empty;
}
