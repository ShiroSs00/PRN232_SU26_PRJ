using Payment.Application.Common;
using Payment.Application.DTOs;
using Payment.Application.DTOs.Payments;

namespace Payment.Application.Abstractions;

public interface IPaymentService
{
    Task<Result<PagedResult<PaymentDto>>> GetListAsync(
        string? sessionId,
        string? plateNumber,
        int? status,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<Result<PaymentDto>> GetByIdAsync(string id, CancellationToken ct = default);

    Task<Result<List<PaymentDto>>> GetBySessionAsync(string sessionId, CancellationToken ct = default);

    Task<Result<List<PaymentDto>>> GetBySubscriptionAsync(string subscriptionId, CancellationToken ct = default);

    Task<Result<PaymentDto>> CreateAsync(string createdByUserId, CreatePaymentRequest request, CancellationToken ct = default);

    Task<Result<PaymentDto>> ConfirmAsync(string id, string confirmedByUserId, CancellationToken ct = default);

    Task<Result<PaymentDto>> CancelAsync(string id, CancellationToken ct = default);
}
