using Payment.Application.Common;
using Payment.Application.DTOs.Payments;

namespace Payment.Application.Abstractions;

public interface IPayOsService
{
    Task<Result<PayOsLinkResponse>> CreatePaymentLinkAsync(string paymentId, CancellationToken ct = default);

    Task<Result> HandleWebhookAsync(string rawPayload, CancellationToken ct = default);

    Task<Result<PaymentDto>> CheckPaymentStatusAsync(string paymentId, CancellationToken ct = default);
}
