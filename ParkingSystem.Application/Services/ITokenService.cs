using ParkingSystem.Domain.Entities;

namespace ParkingSystem.Application.Services;

public interface ITokenService
{
    string GenerateToken(User user);
}
