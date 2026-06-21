using Parking.Domain.Enums;

namespace Parking.Application.DTOs.Incidents;

public class IncidentReportDto
{
    public string Id { get; set; } = string.Empty;
    public string BuildingId { get; set; } = string.Empty;
    public string? ParkingSessionId { get; set; }
    public string? ParkingSlotId { get; set; }
    public string? VehicleId { get; set; }
    public string? PlateNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IncidentType Type { get; set; }
    public IncidentStatus Status { get; set; }
    public string ReportedByUserId { get; set; } = string.Empty;
    public string? ResolvedByUserId { get; set; }
    public string? ResolutionNote { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateIncidentRequest
{
    public string BuildingId { get; set; } = string.Empty;
    public string? ParkingSessionId { get; set; }
    public string? ParkingSlotId { get; set; }
    public string? VehicleId { get; set; }
    public string? PlateNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Type { get; set; } = (int)IncidentType.Other;
}

public class UpdateIncidentRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public int? Type { get; set; }
    public int? Status { get; set; }
    public string? ParkingSessionId { get; set; }
    public string? ParkingSlotId { get; set; }
    public string? VehicleId { get; set; }
    public string? PlateNumber { get; set; }
}

public class ResolveIncidentRequest
{
    public string ResolutionNote { get; set; } = string.Empty;
}

public class IncidentListQuery
{
    public string? BuildingId { get; set; }
    public int? Status { get; set; }
    public int? Type { get; set; }
    public string? PlateNumber { get; set; }
    public string? VehicleId { get; set; }
    public string? ParkingSessionId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
