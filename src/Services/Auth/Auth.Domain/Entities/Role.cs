using Shared.Common.Entities;

namespace Auth.Domain.Entities;

public class Role : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public List<string> Permissions { get; set; } = [];
}
