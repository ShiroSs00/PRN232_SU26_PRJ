using Parking.Application.Common;
using Parking.Application.DTOs;
using Parking.Application.DTOs.Shifts;

namespace Parking.Application.Abstractions;

public interface IShiftService
{
    Task<Result<PagedResult<ShiftDto>>> GetListAsync(
        ShiftListQuery query,
        CancellationToken ct = default);

    Task<Result<ShiftDto>> GetByIdAsync(
        string id,
        CancellationToken ct = default);

    Task<Result<ShiftDto>> GetCurrentAsync(
        string staffUserId,
        CancellationToken ct = default);

    Task<Result<ShiftDto>> OpenAsync(
        string staffUserId,
        OpenShiftRequest request,
        CancellationToken ct = default);

    Task<Result<ShiftDto>> CloseAsync(
        string id,
        string requestedByUserId,
        bool canManageAll,
        CloseShiftRequest request,
        CancellationToken ct = default);
}
