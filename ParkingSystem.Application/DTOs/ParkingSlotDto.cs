namespace ParkingSystem.Application.DTOs;

public class ParkingSlotDto
{
    public string Id { get; set; } = null!;
    public string BuildingId { get; set; } = null!;
    public string FloorId { get; set; } = null!;
    public string ZoneId { get; set; } = null!;
    public string VehicleTypeId { get; set; } = null!;
    public string Code { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Row { get; set; }
    public int Column { get; set; }
    public double PositionX { get; set; }
    public double PositionY { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }

    // Denormalized display names
    public string? BuildingName { get; set; }
    public string? FloorName { get; set; }
    public string? ZoneName { get; set; }
    public string? VehicleTypeName { get; set; }

    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
