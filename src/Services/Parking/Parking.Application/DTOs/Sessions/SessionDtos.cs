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

    public bool IsMonthly { get; set; }

    public string? SubscriptionId { get; set; }

    public string? CheckInNote { get; set; }
}

public class CheckOutRequest
{
    public string? ExitGate { get; set; }

    public decimal TotalFee { get; set; }

    public string? CheckOutNote { get; set; }

    public string? PaymentId { get; set; }
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
