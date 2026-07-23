using Parking.Application.Common;
using Xunit;

namespace Parking.Application.Tests;

public class ShiftReconciliationRulesTests
{
    [Fact]
    public void Calculate_separates_cash_and_non_cash_totals()
    {
        var totals = ShiftReconciliationRules.Calculate(
            cashAmount: 1_250_000m,
            nonCashAmount: 500_000m,
            countedCashAmount: 1_240_000m);

        Assert.Equal(1_250_000m, totals.ExpectedCashAmount);
        Assert.Equal(1_750_000m, totals.TotalPayments);
        Assert.Equal(500_000m, totals.TotalNonCashAmount);
        Assert.Equal(-10_000m, totals.DifferenceAmount);
    }

    [Theory]
    [InlineData(1_250_000, 1_240_000, -10_000)]
    [InlineData(1_250_000, 1_260_000, 10_000)]
    [InlineData(1_250_000, 1_250_000, 0)]
    public void Calculate_uses_counted_minus_expected_for_difference(
        decimal expected,
        decimal counted,
        decimal difference)
    {
        var totals = ShiftReconciliationRules.Calculate(expected, 0, counted);

        Assert.Equal(difference, totals.DifferenceAmount);
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(1, true)]
    [InlineData(0, false)]
    public void RequiresDifferenceNote_only_when_difference_is_non_zero(
        decimal difference,
        bool expected)
    {
        Assert.Equal(
            expected,
            ShiftReconciliationRules.RequiresDifferenceNote(difference));
    }
}
