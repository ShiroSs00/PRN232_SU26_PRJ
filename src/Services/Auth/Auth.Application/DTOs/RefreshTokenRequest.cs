using System.ComponentModel.DataAnnotations;

namespace Auth.Application.DTOs;

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
