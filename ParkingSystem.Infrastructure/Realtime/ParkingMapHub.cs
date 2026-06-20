using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace ParkingSystem.Infrastructure.Realtime;

[Authorize]
public class ParkingMapHub : Hub
{
    public Task SubscribeFloor(string floorId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, $"floor:{floorId}");
    }

    public Task UnsubscribeFloor(string floorId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, $"floor:{floorId}");
    }
}
