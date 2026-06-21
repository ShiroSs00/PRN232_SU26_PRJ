using Parking.Application.Common;
using Parking.Application.DTOs;
using Parking.Application.DTOs.Slots;

namespace Parking.Application.Abstractions;

/// <summary>
/// Quản lý slot đỗ xe: CRUD, đổi vị trí trên lưới, sinh lưới hàng loạt và đổi trạng thái thủ công.
/// </summary>
public interface IParkingSlotService
{
    Task<Result<PagedResult<SlotDto>>> GetListAsync(
        string? buildingId,
        string? floorId,
        string? zoneId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<Result<SlotDto>> GetByIdAsync(string id, CancellationToken ct = default);

    Task<Result<SlotDto>> CreateAsync(CreateSlotRequest request, CancellationToken ct = default);

    Task<Result<SlotDto>> UpdateAsync(string id, UpdateSlotRequest request, CancellationToken ct = default);

    Task<Result> DeleteAsync(string id, CancellationToken ct = default);

    Task<Result<SlotDto>> UpdatePositionAsync(string id, UpdateSlotPositionRequest request, CancellationToken ct = default);

    Task<Result<List<SlotDto>>> GenerateGridAsync(GenerateGridRequest request, CancellationToken ct = default);

    Task<Result<SlotDto>> UpdateStatusAsync(string id, UpdateSlotStatusRequest request, CancellationToken ct = default);
}
