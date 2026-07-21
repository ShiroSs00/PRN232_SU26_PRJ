using MongoDB.Bson.Serialization.Attributes;
using Payment.Domain.Enums;
using Shared.Common.Entities;

namespace Payment.Domain.Entities;

public class Payment : BaseEntity
{
    public string? ParkingSessionId { get; set; }

    public string? SubscriptionId { get; set; }

    public string PlateNumber { get; set; } = string.Empty;

    public string? VehicleId { get; set; }

    public string? ShiftId { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;

    public string? ConfirmedByUserId { get; set; }

    public string? TransactionCode { get; set; }

    public decimal Amount { get; set; }

    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    /// <summary>PayOS orderCode (Int64) — set when a PayOS payment link is created.</summary>
    [BsonIgnoreIfNull]
    public long? OrderCode { get; set; }

    /// <summary>PayOS paymentLinkId returned from /v2/payment-requests.</summary>
    public string? PaymentLinkId { get; set; }

    /// <summary>PayOS hosted checkout URL.</summary>
    public string? CheckoutUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? PaidAt { get; set; }

    public DateTime? RefundedAt { get; set; }

    public string? Note { get; set; }
}
