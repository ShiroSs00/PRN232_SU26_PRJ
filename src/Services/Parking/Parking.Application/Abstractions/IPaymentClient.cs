using Parking.Application.Common;

namespace Parking.Application.Abstractions;

public sealed class CreateParkingPaymentCommand
{
    public string ParkingSessionId { get; init; } = string.Empty;
    public string PlateNumber { get; init; } = string.Empty;
    public string? VehicleId { get; init; }
    public string? OwnerUserId { get; init; }
    public decimal Amount { get; init; }
    public int Method { get; init; }
    public string? ShiftId { get; init; }
}

public sealed class ParkingPaymentDto
{
    public string Id { get; set; } = string.Empty;
    public string? ParkingSessionId { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string? OwnerUserId { get; set; }
    public decimal Amount { get; set; }
    public int Method { get; set; }
    public int Status { get; set; }
    public string? ShiftId { get; set; }
}

public sealed class ShiftPaymentSummaryDto
{
    public string ShiftId { get; set; } = string.Empty;
    public decimal CashAmount { get; set; }
    public decimal NonCashAmount { get; set; }
    public long PendingPaymentCount { get; set; }
}

public static class ParkingPaymentStatus
{
    public const int Pending = 1;
    public const int Paid = 2;
    public const int Failed = 3;
    public const int Refunded = 4;
    public const int Cancelled = 5;
}

public static class ParkingPaymentMethod
{
    public const int Cash = 1;
}

public interface IPaymentClient
{
    Task<Result<ParkingPaymentDto>> CreateForParkingSessionAsync(
        CreateParkingPaymentCommand command,
        CancellationToken ct = default);

    Task<Result<ParkingPaymentDto>> GetByIdAsync(
        string paymentId,
        CancellationToken ct = default);

    Task<Result<ShiftPaymentSummaryDto>> GetShiftSummaryAsync(
        string shiftId,
        CancellationToken ct = default);
}
