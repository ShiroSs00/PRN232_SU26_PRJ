using Parking.Domain.Enums;
using Shared.Common.Entities;

namespace Parking.Domain.Entities;

public class ParkingSessionLog : BaseEntity
{
    public string ParkingSessionId { get; set; } = string.Empty;

    public ParkingSessionLogAction Action { get; set; }

    public string? FromParkingSlotId { get; set; }

    public string? ToParkingSlotId { get; set; }

    public string Description { get; set; } = string.Empty;

    public string CreatedByUserId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
