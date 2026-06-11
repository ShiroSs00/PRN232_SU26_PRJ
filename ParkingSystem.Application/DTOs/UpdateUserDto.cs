using System.ComponentModel.DataAnnotations;
using ParkingSystem.Application.Validation;

namespace ParkingSystem.Application.DTOs;

public class UpdateUserDto
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters.")]
    public string FullName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Invalid email address.")]
    public string? Email { get; set; }

    [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
    public string? Password { get; set; }

    public string? PhoneNumber { get; set; }

    [ValidRoles(AllowEmpty = true)]
    public List<string> Roles { get; set; } = [];

    public bool? IsActive { get; set; }
}
