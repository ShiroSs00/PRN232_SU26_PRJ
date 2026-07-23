using Parking.Application.Abstractions;
using Parking.Application.Common;
using Xunit;

namespace Parking.Application.Tests;

public class MonthlySubscriptionValidatorTests
{
    private static readonly DateTime Now = new(2026, 7, 23, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Matching_active_subscription_is_eligible()
    {
        var eligible = MonthlySubscriptionValidator.IsEligible(
            CreateSubscription(), "51a-123.45", "building-1", "car", Now);

        Assert.True(eligible);
    }

    [Theory]
    [InlineData("building-2", "car")]
    [InlineData("building-1", "motorbike")]
    public void Building_or_vehicle_type_mismatch_is_not_eligible(string buildingId, string vehicleTypeId)
    {
        var eligible = MonthlySubscriptionValidator.IsEligible(
            CreateSubscription(), "51A12345", buildingId, vehicleTypeId, Now);

        Assert.False(eligible);
    }

    [Fact]
    public void Expired_subscription_is_not_eligible()
    {
        var subscription = CreateSubscription();
        subscription.EndDate = Now.AddSeconds(-1);

        Assert.False(MonthlySubscriptionValidator.IsEligible(
            subscription, "51A12345", "building-1", "car", Now));
    }

    [Fact]
    public void Non_active_subscription_is_not_eligible()
    {
        var subscription = CreateSubscription();
        subscription.Status = 3;

        Assert.False(MonthlySubscriptionValidator.IsEligible(
            subscription, "51A12345", "building-1", "car", Now));
    }

    private static ActiveSubscriptionDto CreateSubscription() => new()
    {
        Id = "subscription-1",
        PlateNumber = "51A 12345",
        BuildingId = "building-1",
        VehicleTypeId = "car",
        Status = 1,
        StartDate = Now.AddMonths(-1),
        EndDate = Now.AddMonths(1)
    };
}
