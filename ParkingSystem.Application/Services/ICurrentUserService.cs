namespace ParkingSystem.Application.Services;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? Email { get; }
    bool IsInRole(string role);
    IEnumerable<string> Roles { get; }
}
