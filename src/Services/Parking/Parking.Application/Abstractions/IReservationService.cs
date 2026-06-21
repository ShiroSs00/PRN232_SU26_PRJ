using Parking.Application.Common;
using Parking.Application.DTOs;
using Parking.Application.DTOs.Reservations;

namespace Parking.Application.Abstractions;

/// <summary>
/// Quản lý đặt chỗ trước: tạo, xác nhận, huỷ, check-in (đánh dấu) và hết hạn reservation.
/// Cập nhật trạng thái slot và đẩy realtime qua IParkingMapNotifier khi slot chuyển Reserved/Available.
/// </summary>
public interface IReservationService
{
    Task<Result<PagedResult<ReservationDto>>> GetListAsync(
        ReservationListQuery query,
        CancellationToken ct = default);

    Task<Result<ReservationDto>> GetByIdAsync(string id, CancellationToken ct = default);

    Task<Result<ReservationDto>> CreateAsync(
        CreateReservationRequest request,
        string? driverUserId,
        CancellationToken ct = default);

    Task<Result<ReservationDto>> UpdateAsync(
        string id,
        UpdateReservationRequest request,
        CancellationToken ct = default);

    Task<Result<ReservationDto>> ConfirmAsync(string id, CancellationToken ct = default);

    Task<Result<ReservationDto>> CancelAsync(
        string id,
        CancelReservationRequest request,
        string? cancelledByUserId,
        bool enforceOwnership,
        CancellationToken ct = default);

    Task<Result<ReservationDto>> CheckInAsync(
        string id,
        CheckInReservationRequest request,
        CancellationToken ct = default);

    Task<Result<ReservationDto>> ExpireAsync(string id, CancellationToken ct = default);
}
