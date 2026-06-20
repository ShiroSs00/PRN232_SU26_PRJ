using System;

namespace ParkingSystem.Application.DTOs;

public class SlotStatusChangedEvent
{
    public string FloorId { get; set; } = string.Empty;
    public string SlotId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public OccupyingVehicleDto? Vehicle { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
