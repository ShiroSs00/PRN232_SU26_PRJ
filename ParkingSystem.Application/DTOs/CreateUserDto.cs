using System.ComponentModel.DataAnnotations;
using ParkingSystem.Application.Validation;

namespace ParkingSystem.Application.DTOs;

public class CreateUserDto
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
    public string Password { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "At least one role is required.")]
    [ValidRoles(AllowEmpty = false)]
    public List<string> Roles { get; set; } = [];
}
