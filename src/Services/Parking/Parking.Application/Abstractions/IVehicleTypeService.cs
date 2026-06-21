using Parking.Application.Common;
using Parking.Application.DTOs;
using Parking.Application.DTOs.MasterData;

namespace Parking.Application.Abstractions;

public interface IVehicleTypeService
{
    Task<Result<PagedResult<VehicleTypeDto>>> GetListAsync(string? search, bool? isActive, int page, int pageSize, CancellationToken ct = default);
    Task<Result<VehicleTypeDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<VehicleTypeDto>> CreateAsync(CreateVehicleTypeRequest request, CancellationToken ct = default);
    Task<Result<VehicleTypeDto>> UpdateAsync(string id, UpdateVehicleTypeRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(string id, CancellationToken ct = default);
}
