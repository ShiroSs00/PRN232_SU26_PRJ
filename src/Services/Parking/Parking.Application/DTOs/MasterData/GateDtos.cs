using Parking.Domain.Enums;

namespace Parking.Application.DTOs.MasterData;

public class GateDto
{
    public string Id { get; set; } = string.Empty;
    public string BuildingId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public GateType Type { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateGateRequest
{
    public string BuildingId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public GateType Type { get; set; } = GateType.Both;
}

public class UpdateGateRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public GateType Type { get; set; } = GateType.Both;
    public bool IsActive { get; set; } = true;
}
