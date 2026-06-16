using Shared.Common.Entities;

namespace Parking.Domain.Entities;

public class Building : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? PhoneNumber { get; set; }

    public TimeOnly? OpeningTime { get; set; }

    public TimeOnly? ClosingTime { get; set; }
}
