using Shared.Common.Entities;

namespace Parking.Domain.Entities;

public class VehicleType : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}
