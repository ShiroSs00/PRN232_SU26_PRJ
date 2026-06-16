using Parking.Domain.Enums;
using Shared.Common.Entities;

namespace Parking.Domain.Entities;

public class Gate : AuditableEntity
{
    public string BuildingId { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public GateType Type { get; set; } = GateType.Both;
}
