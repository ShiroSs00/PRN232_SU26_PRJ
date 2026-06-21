using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Parking.API.Hubs;

/// <summary>
/// Hub realtime cho map bãi đỗ. Client join group theo từng tầng để chỉ nhận
/// event của tầng đang xem. Event đẩy về client mang tên "SlotStatusChanged".
/// </summary>
[Authorize(Roles = "Admin,FacilityManager,ParkingStaff,Driver")]
public class ParkingMapHub : Hub
{
    public static string FloorGroup(string floorId) => $"floor:{floorId}";

    /// <summary>Client gọi khi mở map một tầng để nhận realtime cho tầng đó.</summary>
    public Task SubscribeFloor(string floorId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, FloorGroup(floorId));

    /// <summary>Client gọi khi rời map một tầng.</summary>
    public Task UnsubscribeFloor(string floorId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, FloorGroup(floorId));
}
