using Shared.Common.Entities;

namespace Parking.Domain.Entities;

public class Floor : AuditableEntity
{
    public string BuildingId { get; set; } = string.Empty;

    public int FloorNumber { get; set; }

    public string Name { get; set; } = string.Empty;
}
