using System.Threading.Tasks;
using ParkingSystem.Application.DTOs;

namespace ParkingSystem.Application.Services;

public interface IParkingMapService
{
    Task<FloorMapDto?> GetFloorMapAsync(string floorId);
    Task<bool> UpdateSlotPositionAsync(string slotId, UpdateSlotPositionDto dto);
    Task<bool> GenerateGridLayoutAsync(string floorId, GenerateGridLayoutDto dto);
}
