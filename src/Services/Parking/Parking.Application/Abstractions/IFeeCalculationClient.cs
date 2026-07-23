using Parking.Application.Common;

namespace Parking.Application.Abstractions;

public interface IFeeCalculationClient
{
    Task<Result<CalculatedParkingFee>> CalculateAsync(
        string buildingId,
        string vehicleTypeId,
        DateTime checkInTime,
        DateTime checkOutTime,
        bool penaltiesOnly,
        bool isLostTicket,
        CancellationToken ct = default);
}

public class CalculatedParkingFee
{
    public decimal Amount { get; set; }

    public string FeePolicyId { get; set; } = string.Empty;

    public TimeSpan Duration { get; set; }
}
