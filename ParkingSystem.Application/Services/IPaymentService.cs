using ParkingSystem.Application.DTOs;

namespace ParkingSystem.Application.Services;

public interface IPaymentService
{
    Task<IEnumerable<PaymentDto>> GetAllAsync(string? status = null, string? method = null);
    Task<PaymentDto?> GetByIdAsync(string id);
    Task<PaymentDto?> GetBySessionIdAsync(string sessionId);
    Task<PaymentDto> CreateAsync(CreatePaymentDto dto);
    Task<PaymentDto?> ConfirmPaymentAsync(string id, ConfirmPaymentDto dto);
    Task<PaymentDto?> RefundPaymentAsync(string id);
}
