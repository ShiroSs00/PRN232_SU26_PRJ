using Parking.Application.Common;
using Parking.Application.DTOs;
using Parking.Application.DTOs.Sessions;

namespace Parking.Application.Abstractions;

public interface IParkingSessionService
{
    Task<Result<PagedResult<ParkingSessionDto>>> GetListAsync(
        SessionListQuery query,
        CancellationToken ct = default);

    /// <summary>Lượt gửi của một tài xế: các phiên có biển số trùng xe đã đăng ký bởi driver.</summary>
    Task<Result<PagedResult<ParkingSessionDto>>> GetMySessionsAsync(
        string driverUserId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<Result<ParkingSessionDto>> GetByIdAsync(string id, CancellationToken ct = default);

    Task<Result<ParkingSessionDto>> GetActiveByPlateAsync(string plateNumber, CancellationToken ct = default);

    /// <summary>
    /// Phí tạm tính cho một phiên đang gửi (Active), tính từ giờ vào tới hiện tại.
    /// Khi <paramref name="enforceOwnership"/> = true (caller là Driver), chỉ cho xem phí
    /// của phiên gắn với xe mà driver đã đăng ký; ngược lại trả SessionAccessDenied.
    /// </summary>
    Task<Result<EstimateFeeResponse>> EstimateFeeAsync(
        string id,
        string? requestUserId,
        bool enforceOwnership,
        CancellationToken ct = default);

    Task<Result<ParkingSessionDto>> CheckInAsync(
        CheckInRequest request,
        string userId,
        CancellationToken ct = default);

    Task<Result<CheckoutResponse>> PrepareCheckOutAsync(
        string id,
        CheckOutRequest request,
        string userId,
        CancellationToken ct = default);

    Task<Result<CheckoutResponse>> FinalizeCheckOutAsync(
        string id,
        string userId,
        CancellationToken ct = default);

    Task<Result<ParkingSessionDto>> ChangeSlotAsync(
        string id,
        ChangeSlotRequest request,
        string userId,
        CancellationToken ct = default);

    /// <summary>Sửa thông tin xe (biển số/loại xe) của phiên đang gửi.</summary>
    Task<Result<ParkingSessionDto>> UpdateInfoAsync(
        string id,
        UpdateSessionInfoRequest request,
        string userId,
        CancellationToken ct = default);

    /// <summary>Đánh dấu phiên là ngoại lệ (quá hạn / bất thường).</summary>
    Task<Result<ParkingSessionDto>> MarkExceptionAsync(
        string id,
        MarkExceptionRequest request,
        string userId,
        CancellationToken ct = default);
}
