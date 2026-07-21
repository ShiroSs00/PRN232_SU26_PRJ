using Payment.Domain.Enums;

namespace Payment.Application.DTOs.Payments;

public class PaymentDto
{
    public string Id { get; set; } = string.Empty;

    public string? ParkingSessionId { get; set; }

    public string? SubscriptionId { get; set; }

    public string PlateNumber { get; set; } = string.Empty;

    public string? VehicleId { get; set; }

    public string? ShiftId { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;

    public string? ConfirmedByUserId { get; set; }

    public string? TransactionCode { get; set; }

    public decimal Amount { get; set; }

    public PaymentMethod Method { get; set; }

    public PaymentStatus Status { get; set; }

    public long? OrderCode { get; set; }

    public string? PaymentLinkId { get; set; }

    public string? CheckoutUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime? RefundedAt { get; set; }

    public string? Note { get; set; }
}

public class CreatePaymentRequest
{
    public string? ParkingSessionId { get; set; }

    public string? SubscriptionId { get; set; }

    public string PlateNumber { get; set; } = string.Empty;

    public string? VehicleId { get; set; }

    public string? ShiftId { get; set; }

    public decimal Amount { get; set; }

    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;

    public string? Note { get; set; }
}

public class PayOsLinkResponse
{
    public string PaymentId { get; set; } = string.Empty;

    public long OrderCode { get; set; }

    public string CheckoutUrl { get; set; } = string.Empty;

    public string QrCode { get; set; } = string.Empty;

    public string PaymentLinkId { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Description { get; set; } = string.Empty;
}
