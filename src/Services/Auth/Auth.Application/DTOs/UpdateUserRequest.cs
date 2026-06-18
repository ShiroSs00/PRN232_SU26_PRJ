using System.ComponentModel.DataAnnotations;

namespace Auth.Application.DTOs;

public class UpdateUserRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Phone]
    public string? PhoneNumber { get; set; }

    public string? AvatarUrl { get; set; }

    /// <summary>
    /// Replaces the user's roles. Must be a non-empty list of valid role names.
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "At least one role is required.")]
    public List<string> Roles { get; set; } = new();

    public bool IsActive { get; set; } = true;
}
