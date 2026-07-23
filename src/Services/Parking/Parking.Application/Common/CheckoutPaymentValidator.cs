using Parking.Application.Abstractions;

namespace Parking.Application.Common;

public static class CheckoutPaymentValidator
{
    public static bool Matches(
        ParkingPaymentDto payment,
        string parkingSessionId,
        string normalizedPlateNumber,
        decimal expectedAmount) =>
        payment.ParkingSessionId == parkingSessionId
        && payment.Amount == expectedAmount
        && PlateNumberNormalizer.Normalize(payment.PlateNumber) == normalizedPlateNumber;
}
