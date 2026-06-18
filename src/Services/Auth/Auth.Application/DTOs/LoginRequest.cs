using System.ComponentModel.DataAnnotations;

namespace Auth.Application.DTOs;

public class LoginRequest
{
    /// <summary>
    /// Username or email.
    /// </summary>
    [Required]
    public string UsernameOrEmail { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
