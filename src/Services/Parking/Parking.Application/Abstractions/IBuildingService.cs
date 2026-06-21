using Parking.Application.Common;
using Parking.Application.DTOs;
using Parking.Application.DTOs.MasterData;

namespace Parking.Application.Abstractions;

public interface IBuildingService
{
    Task<Result<PagedResult<BuildingDto>>> GetListAsync(string? search, bool? isActive, int page, int pageSize, CancellationToken ct = default);
    Task<Result<BuildingDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<BuildingDto>> CreateAsync(CreateBuildingRequest request, CancellationToken ct = default);
    Task<Result<BuildingDto>> UpdateAsync(string id, UpdateBuildingRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(string id, CancellationToken ct = default);
}
