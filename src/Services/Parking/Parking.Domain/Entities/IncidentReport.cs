using Parking.Domain.Enums;
using Shared.Common.Entities;

namespace Parking.Domain.Entities;

public class IncidentReport : AuditableEntity
{
    public string BuildingId { get; set; } = string.Empty;

    public string? ParkingSessionId { get; set; }

    public string? ParkingSlotId { get; set; }

    public string? VehicleId { get; set; }

    public string? PlateNumber { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public IncidentStatus Status { get; set; } = IncidentStatus.Open;

    public string ReportedByUserId { get; set; } = string.Empty;

    public string? ResolvedByUserId { get; set; }

    public DateTime? ResolvedAt { get; set; }
}
