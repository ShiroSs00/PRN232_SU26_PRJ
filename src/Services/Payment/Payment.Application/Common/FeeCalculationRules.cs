using Payment.Domain.Enums;

namespace Payment.Application.Common;

public static class FeeCalculationRules
{
    public static bool IncludeBaseCharge(bool penaltiesOnly) => !penaltiesOnly;

    public static bool ShouldApplyOvertime(
        PricingType pricingType,
        decimal overtimeFee,
        int? overtimeAfterHours,
        TimeSpan duration,
        bool penaltiesOnly)
    {
        if (overtimeFee <= 0)
            return false;
        if (!penaltiesOnly && pricingType != PricingType.PerTurn && pricingType != PricingType.Hourly)
            return false;

        var threshold = overtimeAfterHours ?? 24;
        return duration.TotalHours > threshold;
    }
}
