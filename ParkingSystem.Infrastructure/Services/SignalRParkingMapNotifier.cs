using Microsoft.AspNetCore.SignalR;
using ParkingSystem.Application.DTOs;
using ParkingSystem.Application.Services;
using ParkingSystem.Infrastructure.Realtime;
using System.Threading.Tasks;

namespace ParkingSystem.Infrastructure.Services;

public class SignalRParkingMapNotifier : IParkingMapNotifier
{
    private readonly IHubContext<ParkingMapHub> _hubContext;

    public SignalRParkingMapNotifier(IHubContext<ParkingMapHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifySlotChangedAsync(string floorId, SlotStatusChangedEvent @event)
    {
        await _hubContext.Clients.Group($"floor:{floorId}")
            .SendAsync("SlotStatusChanged", @event);
    }
}
