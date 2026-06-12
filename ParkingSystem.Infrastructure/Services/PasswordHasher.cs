using ParkingSystem.Application.Services;
using BCryptNet = BCrypt.Net.BCrypt;

namespace ParkingSystem.Infrastructure.Services;

public class PasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        return BCryptNet.HashPassword(password);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        return BCryptNet.Verify(password, hashedPassword);
    }
}
