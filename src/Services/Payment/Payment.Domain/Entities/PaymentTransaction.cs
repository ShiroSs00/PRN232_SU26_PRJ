using Payment.Domain.Enums;
using Shared.Common.Entities;

namespace Payment.Domain.Entities;

public class PaymentTransaction : BaseEntity
{
    public string PaymentId { get; set; } = string.Empty;

    public string Provider { get; set; } = string.Empty;

    public string TransactionCode { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public PaymentMethod Method { get; set; } = PaymentMethod.Mock;

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public string? RequestPayload { get; set; }

    public string? ResponsePayload { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }
}
