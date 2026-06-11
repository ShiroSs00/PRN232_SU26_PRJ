using System.ComponentModel.DataAnnotations;
using ParkingSystem.Domain.Enums;

namespace ParkingSystem.Application.Validation;

public class ValidRolesAttribute : ValidationAttribute
{
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
            if (!Enum.TryParse<UserRole>(role, out _))
            {
                var allowedRoles = string.Join(", ", Enum.GetNames<UserRole>());
                return new ValidationResult($"Role '{role}' is invalid. Allowed roles are: {allowedRoles}.");
            }
        }

        return ValidationResult.Success;
    }
}
