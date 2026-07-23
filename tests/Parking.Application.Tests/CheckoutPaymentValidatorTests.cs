using Parking.Application.Abstractions;
using Parking.Application.Common;
using Xunit;

namespace Parking.Application.Tests;

public class CheckoutPaymentValidatorTests
{
    [Fact]
    public void Matches_returns_true_for_same_session_amount_and_normalized_plate()
    {
        var payment = CreatePayment();

        var matches = CheckoutPaymentValidator.Matches(
            payment,
            "session-1",
            "51A12345",
            40_000m);

        Assert.True(matches);
    }

    [Theory]
    [InlineData("another-session", "51A12345", 40000)]
    [InlineData("session-1", "30F99999", 40000)]
    [InlineData("session-1", "51A12345", 1000)]
    public void Matches_returns_false_when_immutable_payment_data_differs(
        string sessionId,
        string normalizedPlate,
        decimal amount)
    {
        var payment = CreatePayment();

        var matches = CheckoutPaymentValidator.Matches(
            payment,
            sessionId,
            normalizedPlate,
            amount);

        Assert.False(matches);
    }

    private static ParkingPaymentDto CreatePayment() => new()
    {
        Id = "payment-1",
        ParkingSessionId = "session-1",
        PlateNumber = "51A-123.45",
        Amount = 40_000m,
        Method = 3,
        Status = ParkingPaymentStatus.Paid
    };
}
