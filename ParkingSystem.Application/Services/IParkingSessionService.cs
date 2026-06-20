using ParkingSystem.Application.DTOs;

namespace ParkingSystem.Application.Services;

public interface IParkingSessionService
{
    Task<IEnumerable<ParkingSessionDto>> GetAllAsync(string? status = null, string? plateNumber = null);
    Task<ParkingSessionDto?> GetByIdAsync(string id);
    Task<ParkingSessionDto?> GetActiveByPlateNumberAsync(string plateNumber);
    Task<ParkingSessionDto> CheckInAsync(CheckInDto dto);
    Task<ParkingSessionDto?> CheckOutAsync(string id, CheckOutDto dto);
    Task<ParkingSessionDto?> MarkLostTicketAsync(string id);
    Task<ParkingSessionDto?> CancelAsync(string id);
    Task<ParkingSessionDto?> ConfirmPaymentAsync(string id, ConfirmPaymentDto dto);
}
