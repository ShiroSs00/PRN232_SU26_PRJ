using Payment.Domain.Enums;

namespace Payment.Application.DTOs.Subscriptions;

public class SubscriptionDto
{
    public string Id { get; set; } = string.Empty;

    public string PlateNumber { get; set; } = string.Empty;

    public string? VehicleId { get; set; }

    public string VehicleTypeId { get; set; } = string.Empty;

    public string BuildingId { get; set; } = string.Empty;

    public string OwnerName { get; set; } = string.Empty;

    public string OwnerPhone { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public decimal MonthlyFee { get; set; }

    public SubscriptionStatus Status { get; set; }

    public DateTime? SuspendedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

public class CreateSubscriptionRequest
{
    public string PlateNumber { get; set; } = string.Empty;

    public string? VehicleId { get; set; }

    public string VehicleTypeId { get; set; } = string.Empty;

    public string BuildingId { get; set; } = string.Empty;

    public string OwnerName { get; set; } = string.Empty;

    public string OwnerPhone { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public decimal MonthlyFee { get; set; }

    public string? Note { get; set; }
}

public class UpdateSubscriptionRequest
{
    public string? VehicleId { get; set; }

    public string VehicleTypeId { get; set; } = string.Empty;

    public string BuildingId { get; set; } = string.Empty;

    public string OwnerName { get; set; } = string.Empty;

    public string OwnerPhone { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public decimal MonthlyFee { get; set; }

    public string? Note { get; set; }
}

public class RenewSubscriptionRequest
{
    public int Months { get; set; } = 1;
}
