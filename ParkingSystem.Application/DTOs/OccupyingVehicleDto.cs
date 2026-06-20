namespace ParkingSystem.Application.DTOs;

public class OccupyingVehicleDto
{
    public string SessionId { get; set; } = string.Empty;
    public string PlateNumber { get; set; } = string.Empty;
    public DateTime CheckInTime { get; set; }
    public bool IsMonthly { get; set; }
}
