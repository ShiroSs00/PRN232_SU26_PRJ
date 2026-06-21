using Parking.Domain.Enums;
using Shared.Common.Entities;

namespace Parking.Domain.Entities;

public class ParkingSlot : AuditableEntity
{
    public string BuildingId { get; set; } = string.Empty;

    public string FloorId { get; set; } = string.Empty;

    public string ZoneId { get; set; } = string.Empty;

    public string VehicleTypeId { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string? Label { get; set; }

    public int Row { get; set; }

    public int Column { get; set; }

    public int RowSpan { get; set; } = 1;

    public int ColSpan { get; set; } = 1;

    public SlotStatus Status { get; set; } = SlotStatus.Available;

    public string? CurrentSessionId { get; set; }
}
