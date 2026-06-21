using Microsoft.AspNetCore.SignalR;
using Parking.API.Hubs;
using Parking.Application.Abstractions;
using Parking.Application.DTOs.Realtime;

namespace Parking.API.Realtime;

/// <summary>
/// Triển khai <see cref="IParkingMapNotifier"/> bằng SignalR. Đặt ở tầng API vì
/// chỉ project Web SDK mới có <see cref="IHubContext{THub}"/>. Đẩy event tới đúng
/// group của tầng để client chỉ nhận thay đổi của tầng đang xem.
/// </summary>
public class SignalRParkingMapNotifier : IParkingMapNotifier
{
    private readonly IHubContext<ParkingMapHub> _hubContext;

    public SignalRParkingMapNotifier(IHubContext<ParkingMapHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifySlotChangedAsync(SlotStatusChangedEvent slotEvent, CancellationToken ct = default) =>
        _hubContext.Clients
            .Group(ParkingMapHub.FloorGroup(slotEvent.FloorId))
            .SendAsync("SlotStatusChanged", slotEvent, ct);
}
