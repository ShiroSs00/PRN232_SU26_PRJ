using Parking.Application.DTOs.Realtime;
using Parking.Domain.Enums;

namespace Parking.Application.DTOs.Map;

/// <summary>
/// Toàn bộ dữ liệu để render map dạng lưới cho một tầng: kích thước lưới + danh sách slot + tổng hợp.
/// </summary>
public class FloorMapDto
{
    public string FloorId { get; set; } = string.Empty;

    public string BuildingId { get; set; } = string.Empty;

    public string FloorName { get; set; } = string.Empty;

    public int GridRows { get; set; }

    public int GridCols { get; set; }

    public List<MapSlotDto> Slots { get; set; } = new();

    public MapSummaryDto Summary { get; set; } = new();
}

/// <summary>
/// Một ô slot trên map. Khi Status = Occupied thì Vehicle được điền từ ParkingSession tương ứng.
/// </summary>
public class MapSlotDto
{
    public string SlotId { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string? Label { get; set; }

    public int Row { get; set; }

    public int Column { get; set; }

    public int RowSpan { get; set; } = 1;

    public int ColSpan { get; set; } = 1;

    public string ZoneId { get; set; } = string.Empty;

    public string VehicleTypeId { get; set; } = string.Empty;

    public SlotStatus Status { get; set; }

    public OccupyingVehicleDto? Vehicle { get; set; }
}

/// <summary>
/// Số liệu tổng hợp các slot trên tầng theo trạng thái.
/// </summary>
public class MapSummaryDto
{
    public int Total { get; set; }

    public int Available { get; set; }

    public int Occupied { get; set; }

    public int Reserved { get; set; }

    public int Maintenance { get; set; }
}

/// <summary>
/// Mục tầng rút gọn dùng cho dropdown chọn tầng theo toà nhà.
/// </summary>
public class FloorOptionDto
{
    public string FloorId { get; set; } = string.Empty;

    public string BuildingId { get; set; } = string.Empty;

    public int FloorNumber { get; set; }

    public string Name { get; set; } = string.Empty;

    public int GridRows { get; set; }

    public int GridCols { get; set; }
}
