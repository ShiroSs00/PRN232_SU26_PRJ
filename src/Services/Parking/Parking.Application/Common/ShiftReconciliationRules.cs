namespace Parking.Application.Common;

public sealed record ShiftTotals(
    decimal ExpectedCashAmount,
    decimal TotalPayments,
    decimal TotalNonCashAmount,
    decimal DifferenceAmount);

public static class ShiftReconciliationRules
{
    public static ShiftTotals Calculate(
        decimal cashAmount,
        decimal nonCashAmount,
        decimal countedCashAmount)
    {
        return new ShiftTotals(
            cashAmount,
            cashAmount + nonCashAmount,
            nonCashAmount,
            countedCashAmount - cashAmount);
    }

    public static bool RequiresDifferenceNote(decimal differenceAmount) =>
        differenceAmount != 0;
}
