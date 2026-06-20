using System.Threading.Tasks;
using ParkingSystem.Application.DTOs;

namespace ParkingSystem.Application.Services;

public interface IParkingMapNotifier
{
    Task NotifySlotChangedAsync(string floorId, SlotStatusChangedEvent @event);
}
