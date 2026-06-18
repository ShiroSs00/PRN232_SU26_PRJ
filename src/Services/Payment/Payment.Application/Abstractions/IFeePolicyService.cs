using Payment.Application.Common;
using Payment.Application.DTOs;
using Payment.Application.DTOs.FeePolicies;

namespace Payment.Application.Abstractions;

public interface IFeePolicyService
{
    Task<Result<PagedResult<FeePolicyDto>>> GetListAsync(
        string? buildingId,
        string? vehicleTypeId,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<Result<FeePolicyDto>> GetByIdAsync(string id, CancellationToken ct = default);

    Task<Result<List<FeePolicyDto>>> GetActiveAsync(
        string? buildingId,
        string? vehicleTypeId,
        CancellationToken ct = default);

    Task<Result<FeePolicyDto>> CreateAsync(CreateFeePolicyRequest request, CancellationToken ct = default);

    Task<Result<FeePolicyDto>> UpdateAsync(string id, UpdateFeePolicyRequest request, CancellationToken ct = default);

    Task<Result> DeleteAsync(string id, CancellationToken ct = default);

    Task<Result<CalculateFeeResponse>> CalculateAsync(CalculateFeeRequest request, CancellationToken ct = default);
}
