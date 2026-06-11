using System.ComponentModel.DataAnnotations;
using ParkingSystem.Domain.Constants;

namespace ParkingSystem.Application.Validation;

public class ValidRolesAttribute : ValidationAttribute
{
    private static readonly string[] AllowedRoles = new[]
    {
        UserRoles.Admin,
        UserRoles.FacilityManager,
        UserRoles.ParkingStaff,
        UserRoles.Driver
    };

    public bool AllowEmpty { get; set; } = false;

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
        {
            return AllowEmpty ? ValidationResult.Success : new ValidationResult("Roles list cannot be null.");
        }

        if (value is not List<string> roles)
        {
            return new ValidationResult("Invalid roles format.");
        }

        if (roles.Count == 0)
        {
            return AllowEmpty ? ValidationResult.Success : new ValidationResult("At least one role is required.");
        }

        foreach (var role in roles)
        {
            if (!AllowedRoles.Contains(role))
            {
                return new ValidationResult($"Role '{role}' is invalid. Allowed roles are: {string.Join(", ", AllowedRoles)}.");
            }
        }

        return ValidationResult.Success;
    }
}
