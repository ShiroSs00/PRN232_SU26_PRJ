namespace ParkingSystem.Application.DTOs;

public class LoginResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public UserDto User { get; set; } = null!;
}
