using ParkingSystem.Application.DTOs;

namespace ParkingSystem.Application.Services;

public interface IUserService
{
    Task<LoginResponseDto?> AuthenticateAsync(LoginDto loginDto);
    Task<UserDto?> GetByIdAsync(string id);
    Task<UserDto?> GetByEmailAsync(string email);
    Task<IEnumerable<UserDto>> GetAllAsync();
    Task<UserDto> CreateAsync(CreateUserDto createUserDto);
    Task<UserDto?> UpdateAsync(string id, UpdateUserDto updateUserDto);
    Task<bool> DeleteAsync(string id);
}
