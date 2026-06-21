using Parking.Application.Common;
using Parking.Application.DTOs;
using Parking.Application.DTOs.Vehicles;

namespace Parking.Application.Abstractions;

public interface IVehicleService
{
    Task<Result<PagedResult<VehicleDto>>> GetListAsync(
        string? search,
        string? ownerUserId,
        string? vehicleTypeId,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<Result<VehicleDto>> GetByIdAsync(string id, CancellationToken ct = default);

    Task<Result<VehicleDto>> GetByPlateAsync(string plateNumber, CancellationToken ct = default);

    Task<Result<VehicleDto>> CreateAsync(CreateVehicleRequest request, CancellationToken ct = default);

    Task<Result<VehicleDto>> UpdateAsync(string id, UpdateVehicleRequest request, CancellationToken ct = default);

    Task<Result> DeleteAsync(string id, CancellationToken ct = default);
}
