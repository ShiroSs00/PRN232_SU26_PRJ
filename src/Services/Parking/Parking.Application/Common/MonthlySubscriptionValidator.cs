using Parking.Application.Abstractions;

namespace Parking.Application.Common;

public static class MonthlySubscriptionValidator
{
    private const int ActiveStatus = 1;

    public static bool IsEligible(
        ActiveSubscriptionDto? subscription,
        string plateNumber,
        string buildingId,
        string vehicleTypeId,
        DateTime utcNow)
    {
        if (subscription is null)
            return false;

        return subscription.Status == ActiveStatus &&
               subscription.StartDate <= utcNow &&
               subscription.EndDate >= utcNow &&
               PlateNumberNormalizer.Normalize(subscription.PlateNumber) == PlateNumberNormalizer.Normalize(plateNumber) &&
               string.Equals(subscription.BuildingId, buildingId.Trim(), StringComparison.Ordinal) &&
               string.Equals(subscription.VehicleTypeId, vehicleTypeId.Trim(), StringComparison.Ordinal);
    }
}
