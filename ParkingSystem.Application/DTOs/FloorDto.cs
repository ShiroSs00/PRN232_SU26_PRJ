namespace ParkingSystem.Application.DTOs;

public class FloorDto
{
    public string Id { get; set; } = null!;
    public string BuildingId { get; set; } = string.Empty;
    public string FloorNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int GridRows { get; set; }
    public int GridCols { get; set; }
    public bool IsActive { get; set; }
}
