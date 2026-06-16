using Shared.Common.Entities;

namespace Auth.Domain.Entities;

public class User : AuditableEntity
{
    public string FullName { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string? AvatarUrl { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public List<string> Roles { get; set; } = [];
}
