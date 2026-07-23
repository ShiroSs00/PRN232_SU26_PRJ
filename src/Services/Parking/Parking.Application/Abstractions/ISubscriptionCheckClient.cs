namespace Parking.Application.Abstractions;
using Parking.Application.Common;

public class ActiveSubscriptionDto
{
    public string Id { get; set; } = string.Empty;
    public string BuildingId { get; set; } = string.Empty;
    public string VehicleTypeId { get; set; } = string.Empty;
    public string PlateNumber { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int Status { get; set; }
}

public interface ISubscriptionCheckClient
{
    Task<Result<ActiveSubscriptionDto?>> GetActiveAsync(
        string plateNumber,
        string buildingId,
        string vehicleTypeId,
        CancellationToken ct = default);
}
