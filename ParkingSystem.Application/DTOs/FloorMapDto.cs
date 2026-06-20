using System.Collections.Generic;

namespace ParkingSystem.Application.DTOs;

public class FloorMapDto
{
    public string FloorId { get; set; } = string.Empty;
    public string BuildingId { get; set; } = string.Empty;
    public string FloorName { get; set; } = string.Empty;
    public int GridRows { get; set; }
    public int GridCols { get; set; }
    public List<MapSlotDto> Slots { get; set; } = [];
    public MapSummaryDto Summary { get; set; } = null!;
}
