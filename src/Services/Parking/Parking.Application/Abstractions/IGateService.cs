using Parking.Application.Common;
using Parking.Application.DTOs;
using Parking.Application.DTOs.MasterData;

namespace Parking.Application.Abstractions;

public interface IGateService
{
    Task<Result<PagedResult<GateDto>>> GetListAsync(string? buildingId, bool? isActive, int page, int pageSize, CancellationToken ct = default);
    Task<Result<GateDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<GateDto>> CreateAsync(CreateGateRequest request, CancellationToken ct = default);
    Task<Result<GateDto>> UpdateAsync(string id, UpdateGateRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(string id, CancellationToken ct = default);
}
