using Parking.Domain.Enums;

namespace Parking.Application.DTOs.Sessions;

public class ParkingSessionDto
{
    public string Id { get; set; } = string.Empty;

    public string PlateNumber { get; set; } = string.Empty;

    public string VehicleTypeId { get; set; } = string.Empty;

    public string? VehicleId { get; set; }

    public string BuildingId { get; set; } = string.Empty;

    public string ZoneId { get; set; } = string.Empty;

    public string ParkingSlotId { get; set; } = string.Empty;

    public string? ShiftId { get; set; }

    public string? PaymentId { get; set; }

    public string? ReservationId { get; set; }

    public DateTime CheckInTime { get; set; }

    public DateTime? CheckOutTime { get; set; }

    public string? EntryGate { get; set; }

    public string? ExitGate { get; set; }

    public string? CheckInNote { get; set; }

    public string? CheckOutNote { get; set; }

    public ParkingSessionStatus Status { get; set; }

    public bool IsMonthly { get; set; }

    public string? SubscriptionId { get; set; }

    public bool IsLostTicket { get; set; }

    public decimal TotalFee { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;

    public string? CompletedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

public class CheckInRequest
{
    public string PlateNumber { get; set; } = string.Empty;

    public string VehicleTypeId { get; set; } = string.Empty;

    public string BuildingId { get; set; } = string.Empty;

    public string ZoneId { get; set; } = string.Empty;

    /// <summary>Optional. If provided, this exact slot is used (must be Available, or Reserved by the matching reservation). Otherwise an available slot is auto-selected.</summary>
    public string? ParkingSlotId { get; set; }

    /// <summary>Optional. If provided, check-in fulfils this reservation: its reserved slot (Reserved status) is accepted, and the reservation is marked CheckedIn.</summary>
    public string? ReservationId { get; set; }

    public string? EntryGate { get; set; }

    /// <summary>Legacy input retained for compatibility; ignored because monthly status is resolved server-side.</summary>
    public bool IsMonthly { get; set; }

    /// <summary>Legacy input retained for compatibility; ignored because subscription identity is resolved server-side.</summary>
    public string? SubscriptionId { get; set; }

    public string? CheckInNote { get; set; }
}

public class CheckOutRequest
{
    public string? ExitGate { get; set; }

    public bool IsLostTicket { get; set; }

    /// <summary>PaymentMethod numeric value: Cash=1, Card=2, EWallet=3, Mock=4.</summary>
    public int PaymentMethod { get; set; } = 1;

    public string? CheckOutNote { get; set; }
}

public class CheckoutResponse
{
    public ParkingSessionDto Session { get; set; } = new();

    public DateTime CheckoutTime { get; set; }

    public decimal Amount { get; set; }

    public string? PaymentId { get; set; }

    public int? PaymentStatus { get; set; }

    public bool RequiresPayment { get; set; }

    public bool Finalized { get; set; }
}

public class ChangeSlotRequest
{
    public string NewParkingSlotId { get; set; } = string.Empty;
}

/// <summary>Sửa thông tin xe của phiên đang gửi (nhập nhầm biển số / loại xe khi check-in).</summary>
public class UpdateSessionInfoRequest
{
    public string? PlateNumber { get; set; }
    public string? VehicleTypeId { get; set; }
    public string? Note { get; set; }
}

/// <summary>Đánh dấu phiên là ngoại lệ (quá hạn, bất thường) kèm ghi chú.</summary>
public class MarkExceptionRequest
{
    public string? Note { get; set; }
}

/// <summary>Phí tạm tính cho một phiên đang gửi (Active), tính từ giờ vào tới thời điểm hiện tại.</summary>
public class EstimateFeeResponse
{
    public string SessionId { get; set; } = string.Empty;

    public string PlateNumber { get; set; } = string.Empty;

    public string VehicleTypeId { get; set; } = string.Empty;

    public DateTime CheckInTime { get; set; }

    public DateTime EstimatedAt { get; set; }

    /// <summary>Thời lượng gửi tính tới EstimatedAt.</summary>
    public TimeSpan Duration { get; set; }

    /// <summary>Phí tạm tính. 0 nếu phiên thuộc vé tháng (IsMonthly).</summary>
    public decimal EstimatedFee { get; set; }

    /// <summary>True nếu là vé tháng nên không phát sinh phí lượt.</summary>
    public bool IsMonthly { get; set; }

    public string FeePolicyId { get; set; } = string.Empty;
}

public class SessionListQuery
{
    public string? BuildingId { get; set; }

    public int? Status { get; set; }

    public string? PlateNumber { get; set; }

    /// <summary>Lọc theo nhiều biển số (dùng cho "lượt gửi của tôi" theo xe đã đăng ký).</summary>
    public List<string>? PlateNumbers { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}
