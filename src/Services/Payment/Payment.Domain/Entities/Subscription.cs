using Payment.Domain.Enums;
using Shared.Common.Entities;

namespace Payment.Domain.Entities;

public class Subscription : AuditableEntity
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

    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

    public DateTime? SuspendedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public string? Note { get; set; }
}
