using Parking.Application.Common;
using Parking.Application.DTOs;
using Parking.Application.DTOs.MasterData;

namespace Parking.Application.Abstractions;

public interface IZoneService
{
    Task<Result<PagedResult<ZoneDto>>> GetListAsync(string? buildingId, string? floorId, string? vehicleTypeId, bool? isActive, int page, int pageSize, CancellationToken ct = default);
    Task<Result<ZoneDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<ZoneDto>> CreateAsync(CreateZoneRequest request, CancellationToken ct = default);
    Task<Result<ZoneDto>> UpdateAsync(string id, UpdateZoneRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(string id, CancellationToken ct = default);
}
