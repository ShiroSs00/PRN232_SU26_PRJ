using ParkingSystem.Application.DTOs;

namespace ParkingSystem.Application.Services;

public interface IParkingSlotService
{
    Task<IEnumerable<ParkingSlotDto>> GetAllAsync(string? buildingId = null, string? floorId = null, string? zoneId = null, string? vehicleTypeId = null, string? status = null);
    Task<ParkingSlotDto?> GetByIdAsync(string id);
    Task<IEnumerable<ParkingSlotDto>> GetByZoneIdAsync(string zoneId);
    Task<ParkingSlotDto> CreateAsync(CreateParkingSlotDto dto);
    Task<ParkingSlotDto?> UpdateAsync(string id, UpdateParkingSlotDto dto);
    Task<ParkingSlotDto?> UpdateStatusAsync(string id, string status);
    Task<bool> DeleteAsync(string id);
}
