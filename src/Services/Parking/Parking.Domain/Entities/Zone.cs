using Shared.Common.Entities;

namespace Parking.Domain.Entities;

public class Zone : AuditableEntity
{
    public string BuildingId { get; set; } = string.Empty;

    public string FloorId { get; set; } = string.Empty;

    public string VehicleTypeId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int Capacity { get; set; }

    public int CurrentOccupancy { get; set; }
}
