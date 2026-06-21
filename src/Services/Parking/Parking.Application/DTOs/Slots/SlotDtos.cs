using Parking.Domain.Enums;

namespace Parking.Application.DTOs.Slots;

public class SlotDto
{
    public string Id { get; set; } = string.Empty;

    public string BuildingId { get; set; } = string.Empty;

    public string FloorId { get; set; } = string.Empty;

    public string ZoneId { get; set; } = string.Empty;

    public string VehicleTypeId { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string? Label { get; set; }

    public int Row { get; set; }

    public int Column { get; set; }

    public int RowSpan { get; set; } = 1;

    public int ColSpan { get; set; } = 1;

    public SlotStatus Status { get; set; }

    public string? CurrentSessionId { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

public class CreateSlotRequest
{
    public string BuildingId { get; set; } = string.Empty;

    public string FloorId { get; set; } = string.Empty;

    public string ZoneId { get; set; } = string.Empty;

    public string VehicleTypeId { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string? Label { get; set; }

    public int Row { get; set; }

    public int Column { get; set; }

    public int RowSpan { get; set; } = 1;

    public int ColSpan { get; set; } = 1;
}

public class UpdateSlotRequest
{
    public string ZoneId { get; set; } = string.Empty;

    public string VehicleTypeId { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string? Label { get; set; }
}

public class UpdateSlotPositionRequest
{
    public int Row { get; set; }

    public int Column { get; set; }

    public int RowSpan { get; set; } = 1;

    public int ColSpan { get; set; } = 1;

    public string? Label { get; set; }
}

public class GenerateGridRequest
{
    public string FloorId { get; set; } = string.Empty;

    public string ZoneId { get; set; } = string.Empty;

    public string VehicleTypeId { get; set; } = string.Empty;

    public int StartRow { get; set; } = 1;

    public int StartColumn { get; set; } = 1;

    public int Rows { get; set; }

    public int Cols { get; set; }

    public string CodePrefix { get; set; } = string.Empty;
}

public class UpdateSlotStatusRequest
{
    public SlotStatus Status { get; set; }
}
