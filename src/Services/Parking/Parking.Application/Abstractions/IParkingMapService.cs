using Parking.Application.Common;
using Parking.Application.DTOs.Map;

namespace Parking.Application.Abstractions;

/// <summary>
/// Xây dữ liệu map dạng lưới cho từng tầng và danh sách tầng cho dropdown.
/// </summary>
public interface IParkingMapService
{
    Task<Result<FloorMapDto>> GetFloorMapAsync(string floorId, CancellationToken ct = default);

    Task<Result<List<FloorOptionDto>>> GetFloorsByBuildingAsync(string buildingId, CancellationToken ct = default);
}
