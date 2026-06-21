using Parking.Domain.Enums;

namespace Parking.Application.DTOs.Realtime;

/// <summary>
/// Thông tin xe đang chiếm một slot. Chỉ điền khi slot ở trạng thái Occupied.
/// Dùng chung giữa Map (hiển thị) và Session/Reservation (phát event realtime).
/// </summary>
public class OccupyingVehicleDto
{
    public string SessionId { get; set; } = string.Empty;

    public string PlateNumber { get; set; } = string.Empty;

    public string VehicleTypeId { get; set; } = string.Empty;

    public DateTime CheckInTime { get; set; }

    public bool IsMonthly { get; set; }
}

/// <summary>
/// Event đẩy về client khi một slot đổi trạng thái (check-in/out, đặt chỗ, bảo trì...).
/// Client SignalR lắng nghe message "SlotStatusChanged" và cập nhật đúng ô trên map.
/// </summary>
public class SlotStatusChangedEvent
{
    public string FloorId { get; set; } = string.Empty;

    public string BuildingId { get; set; } = string.Empty;

    public string SlotId { get; set; } = string.Empty;

    public string SlotCode { get; set; } = string.Empty;

    public SlotStatus Status { get; set; }

    public OccupyingVehicleDto? Vehicle { get; set; }

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
