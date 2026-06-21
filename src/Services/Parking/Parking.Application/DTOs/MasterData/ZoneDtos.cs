namespace Parking.Application.DTOs.MasterData;

public class ZoneDto
{
    public string Id { get; set; } = string.Empty;
    public string BuildingId { get; set; } = string.Empty;
    public string FloorId { get; set; } = string.Empty;
    public string VehicleTypeId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int CurrentOccupancy { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateZoneRequest
{
    public string BuildingId { get; set; } = string.Empty;
    public string FloorId { get; set; } = string.Empty;
    public string VehicleTypeId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
}

public class UpdateZoneRequest
{
    public string VehicleTypeId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public bool IsActive { get; set; } = true;
}
