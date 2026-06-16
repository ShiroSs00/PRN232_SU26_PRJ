using Payment.Domain.Enums;
using Shared.Common.Entities;

namespace Payment.Domain.Entities;

public class Payment : BaseEntity
{
    public string ParkingSessionId { get; set; } = string.Empty;

    public string PlateNumber { get; set; } = string.Empty;

    public string? VehicleId { get; set; }

    public string? ShiftId { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;

    public string? ConfirmedByUserId { get; set; }

    public string? TransactionCode { get; set; }

    public decimal Amount { get; set; }

    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? PaidAt { get; set; }

    public DateTime? RefundedAt { get; set; }

    public string? Note { get; set; }
}
