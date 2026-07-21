namespace Parking.Application.Abstractions;

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
    Task<ActiveSubscriptionDto?> GetActiveByPlateAsync(string plateNumber, CancellationToken ct = default);
}
