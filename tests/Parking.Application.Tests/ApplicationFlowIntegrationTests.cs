using Parking.Application.Abstractions;
using Parking.Application.Common;
using Payment.Application.Common;
using Payment.Domain.Enums;
using Xunit;

namespace Parking.Application.Tests;

/// <summary>
/// Application-layer integration scenarios that compose the Parking and Payment
/// rules in the same order as the HTTP orchestration flow. These tests do not
/// require MongoDB; database-backed E2E tests remain environment-specific.
/// </summary>
public sealed class ApplicationFlowIntegrationTests
{
    [Fact]
    public void Checkout_flow_requires_paid_matching_payment_before_release()
    {
        var payment = new ParkingPaymentDto
        {
            ParkingSessionId = "session-1",
            PlateNumber = "51A-123.45",
            Amount = 40_000m,
            Status = ParkingPaymentStatus.Pending
        };

        Assert.False(CanFinalize(payment, "session-1", "51A12345", 40_000m));

        payment.Status = ParkingPaymentStatus.Paid;

        Assert.True(CanFinalize(payment, "session-1", "51A12345", 40_000m));
    }

    [Fact]
    public void Monthly_penalty_flow_requires_only_penalty_fee_and_paid_payment()
    {
        Assert.False(FeeCalculationRules.IncludeBaseCharge(penaltiesOnly: true));
        Assert.True(FeeCalculationRules.ShouldApplyOvertime(
            PricingType.PerTurn,
            overtimeFee: 20_000m,
            overtimeAfterHours: 24,
            duration: TimeSpan.FromHours(25),
            penaltiesOnly: true));

        var payment = new ParkingPaymentDto
        {
            ParkingSessionId = "monthly-session",
            PlateNumber = "51A12345",
            Amount = 20_000m,
            Status = ParkingPaymentStatus.Paid
        };

        Assert.True(CanFinalize(payment, "monthly-session", "51A12345", 20_000m));
    }

    [Fact]
    public void PayOs_and_shift_reconciliation_keep_the_same_payment_amount()
    {
        var payment = new ParkingPaymentDto
        {
            ParkingSessionId = "session-2",
            PlateNumber = "51A12345",
            Amount = 1_250_000m,
            Status = ParkingPaymentStatus.Pending
        };

        var payOsDecision = PayOsReconciliationRules.EvaluatePaid(
            PaymentStatus.Pending,
            payment.Amount,
            actualAmount: 1_250_000);
        Assert.Equal(PayOsPaidReconciliationDecision.Apply, payOsDecision);

        var totals = ShiftReconciliationRules.Calculate(
            cashAmount: payment.Amount,
            nonCashAmount: 0,
            countedCashAmount: 1_250_000m);
        Assert.Equal(payment.Amount, totals.ExpectedCashAmount);
        Assert.Equal(0, totals.DifferenceAmount);
    }

    private static bool CanFinalize(
        ParkingPaymentDto payment,
        string sessionId,
        string plateNumber,
        decimal amount) =>
        payment.Status == ParkingPaymentStatus.Paid &&
        CheckoutPaymentValidator.Matches(payment, sessionId, plateNumber, amount);
}
