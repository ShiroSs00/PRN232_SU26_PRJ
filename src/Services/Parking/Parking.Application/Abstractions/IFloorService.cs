using Parking.Application.Common;
using Parking.Application.DTOs;
using Parking.Application.DTOs.MasterData;

namespace Parking.Application.Abstractions;

public interface IFloorService
{
    Task<Result<PagedResult<FloorDto>>> GetListAsync(string? buildingId, bool? isActive, int page, int pageSize, CancellationToken ct = default);
    Task<Result<FloorDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<FloorDto>> CreateAsync(CreateFloorRequest request, CancellationToken ct = default);
    Task<Result<FloorDto>> UpdateAsync(string id, UpdateFloorRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(string id, CancellationToken ct = default);
}
