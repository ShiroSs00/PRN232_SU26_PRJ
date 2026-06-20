namespace ParkingSystem.Application.DTOs;

public class MapSlotDto
{
    public string SlotId { get; set; } = null!;
    public string Code { get; set; } = string.Empty;
    public string? Label { get; set; }
    public int Row { get; set; }
    public int Column { get; set; }
    public int RowSpan { get; set; }
    public int ColSpan { get; set; }
    public string ZoneId { get; set; } = string.Empty;
    public string VehicleTypeId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public OccupyingVehicleDto? Vehicle { get; set; }
}
