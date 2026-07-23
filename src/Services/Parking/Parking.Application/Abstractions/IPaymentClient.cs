using Parking.Application.Common;

namespace Parking.Application.Abstractions;

public sealed class CreateParkingPaymentCommand
{
    public string ParkingSessionId { get; init; } = string.Empty;
    public string PlateNumber { get; init; } = string.Empty;
    public string? VehicleId { get; init; }
    public decimal Amount { get; init; }
    public int Method { get; init; }
    public string? ShiftId { get; init; }
}

public sealed class ParkingPaymentDto
{
    public string Id { get; set; } = string.Empty;
    public string? ParkingSessionId { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Method { get; set; }
    public int Status { get; set; }
    public string? ShiftId { get; set; }
}

public static class ParkingPaymentStatus
{
    public const int Pending = 1;
    public const int Paid = 2;
    public const int Failed = 3;
    public const int Refunded = 4;
    public const int Cancelled = 5;
}

public interface IPaymentClient
{
    Task<Result<ParkingPaymentDto>> CreateForParkingSessionAsync(
        CreateParkingPaymentCommand command,
        CancellationToken ct = default);

    Task<Result<ParkingPaymentDto>> GetByIdAsync(
        string paymentId,
        CancellationToken ct = default);
}
