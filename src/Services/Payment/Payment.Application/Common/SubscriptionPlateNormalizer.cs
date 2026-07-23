namespace Payment.Application.Common;

public static class SubscriptionPlateNormalizer
{
    public static string Normalize(string? plateNumber)
    {
        if (string.IsNullOrWhiteSpace(plateNumber))
            return string.Empty;

        return plateNumber
            .Trim()
            .ToUpperInvariant()
            .Replace(" ", string.Empty)
            .Replace("-", string.Empty)
            .Replace(".", string.Empty);
    }
}
