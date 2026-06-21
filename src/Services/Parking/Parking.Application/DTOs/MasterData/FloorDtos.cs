namespace Parking.Application.DTOs.MasterData;

public class FloorDto
{
    public string Id { get; set; } = string.Empty;
    public string BuildingId { get; set; } = string.Empty;
    public int FloorNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public int GridRows { get; set; }
    public int GridCols { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateFloorRequest
{
    public string BuildingId { get; set; } = string.Empty;
    public int FloorNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public int GridRows { get; set; }
    public int GridCols { get; set; }
}

public class UpdateFloorRequest
{
    public int FloorNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public int GridRows { get; set; }
    public int GridCols { get; set; }
    public bool IsActive { get; set; } = true;
}
