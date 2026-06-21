using System.ComponentModel.DataAnnotations;

namespace Auth.Application.DTOs.Roles;

public class RoleDto
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public List<string> Permissions { get; set; } = new();

    public bool IsSystem { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsActive { get; set; }
}

public class CreateRoleRequest
{
    [Required]
    [StringLength(50, MinimumLength = 2)]
    [RegularExpression(@"^[a-zA-Z0-9_.-]+$",
        ErrorMessage = "Role name may only contain letters, digits, dot, dash, underscore.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Description { get; set; }

    public List<string>? Permissions { get; set; }
}

public class UpdateRoleRequest
{
    [StringLength(200)]
    public string? Description { get; set; }

    [Required]
    public List<string> Permissions { get; set; } = new();

    public bool IsActive { get; set; } = true;
}
