using Payment.Domain.Enums;
using Shared.Common.Entities;

namespace Payment.Domain.Entities;

public class SubscriptionPayment : BaseEntity
{
    public string SubscriptionId { get; set; } = string.Empty;

    public string? PaymentId { get; set; }

    public string PlateNumber { get; set; } = string.Empty;

    public string? VehicleId { get; set; }

    public DateTime PeriodStart { get; set; }

    public DateTime PeriodEnd { get; set; }

    public decimal Amount { get; set; }

    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public string CreatedByUserId { get; set; } = string.Empty;

    public string? ConfirmedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? PaidAt { get; set; }

    public string? Note { get; set; }
}
