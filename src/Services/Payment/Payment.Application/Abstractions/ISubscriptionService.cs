using Payment.Application.Common;
using Payment.Application.DTOs;
using Payment.Application.DTOs.Subscriptions;

namespace Payment.Application.Abstractions;

public interface ISubscriptionService
{
    Task<Result<PagedResult<SubscriptionDto>>> GetListAsync(
        int? status,
        string? buildingId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<Result<SubscriptionDto>> GetByIdAsync(string id, CancellationToken ct = default);

    Task<Result<SubscriptionDto?>> GetActiveByPlateAsync(string plateNumber, CancellationToken ct = default);

    Task<Result<SubscriptionDto>> CreateAsync(CreateSubscriptionRequest request, CancellationToken ct = default);

    Task<Result<SubscriptionDto>> UpdateAsync(string id, UpdateSubscriptionRequest request, CancellationToken ct = default);

    Task<Result<SubscriptionDto>> RenewAsync(string id, RenewSubscriptionRequest request, CancellationToken ct = default);

    Task<Result<SubscriptionDto>> SuspendAsync(string id, CancellationToken ct = default);

    Task<Result<SubscriptionDto>> CancelAsync(string id, CancellationToken ct = default);
}
