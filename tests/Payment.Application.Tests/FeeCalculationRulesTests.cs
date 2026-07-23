using Payment.Application.Common;
using Payment.Domain.Enums;
using Xunit;

namespace Payment.Application.Tests;

public class FeeCalculationRulesTests
{
    [Fact]
    public void Penalties_only_excludes_normal_base_charge()
    {
        Assert.False(FeeCalculationRules.IncludeBaseCharge(penaltiesOnly: true));
        Assert.True(FeeCalculationRules.IncludeBaseCharge(penaltiesOnly: false));
    }

    [Fact]
    public void Per_turn_overtime_after_threshold_is_applied()
    {
        Assert.True(FeeCalculationRules.ShouldApplyOvertime(
            PricingType.PerTurn, 20_000m, 24, TimeSpan.FromHours(25), penaltiesOnly: false));
    }

    [Fact]
    public void Overtime_at_threshold_is_not_applied()
    {
        Assert.False(FeeCalculationRules.ShouldApplyOvertime(
            PricingType.Hourly, 20_000m, 24, TimeSpan.FromHours(24), penaltiesOnly: false));
    }

    [Fact]
    public void Normal_monthly_pricing_does_not_get_overtime()
    {
        Assert.False(FeeCalculationRules.ShouldApplyOvertime(
            PricingType.Monthly, 20_000m, 24, TimeSpan.FromHours(48), penaltiesOnly: false));
    }

    [Fact]
    public void Monthly_session_penalties_only_can_get_overtime()
    {
        Assert.True(FeeCalculationRules.ShouldApplyOvertime(
            PricingType.Monthly, 20_000m, 24, TimeSpan.FromHours(48), penaltiesOnly: true));
    }

    [Fact]
    public void Subscription_plate_normalization_matches_parking_format()
    {
        Assert.Equal("51A12345", SubscriptionPlateNormalizer.Normalize("51a-123.45"));
    }
}
