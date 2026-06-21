using Parking.Application.DTOs.Realtime;

namespace Parking.Application.Abstractions;

/// <summary>
/// Trừu tượng để các service (Session, Reservation, Map) đẩy thay đổi trạng thái slot
/// tới client realtime mà không phụ thuộc trực tiếp vào SignalR.
/// Triển khai cụ thể (SignalRParkingMapNotifier) nằm ở tầng Infrastructure.
/// </summary>
public interface IParkingMapNotifier
{
    Task NotifySlotChangedAsync(SlotStatusChangedEvent slotEvent, CancellationToken ct = default);
}
