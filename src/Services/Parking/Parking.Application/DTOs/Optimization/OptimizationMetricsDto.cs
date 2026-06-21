namespace Parking.Application.DTOs.Optimization;

/// <summary>
/// Số liệu tổng hợp về tình hình sử dụng bãi đỗ của một tòa nhà,
/// dùng để hiển thị trực quan và làm đầu vào cho phân tích AI (RQ1–RQ4).
/// </summary>
public class OptimizationMetricsDto
{
    public string BuildingId { get; set; } = string.Empty;
    public string BuildingName { get; set; } = string.Empty;

    public int TotalSlots { get; set; }
    public int AvailableSlots { get; set; }
    public int OccupiedSlots { get; set; }
    public int ReservedSlots { get; set; }
    public int MaintenanceSlots { get; set; }

    /// <summary>Tỷ lệ lấp đầy toàn tòa (%).</summary>
    public double OccupancyRate { get; set; }

    public int ActiveSessions { get; set; }

    public List<FloorUtilizationDto> Floors { get; set; } = new();
    public List<ZoneUtilizationDto> Zones { get; set; } = new();
    public List<VehicleTypeUtilizationDto> VehicleTypes { get; set; } = new();

    /// <summary>Số lượt xe vào theo giờ trong ngày (0–23), tính từ phiên gửi xe.</summary>
    public List<HourlyFlowDto> HourlyCheckIns { get; set; } = new();

    /// <summary>Lưu lượng xe vào theo giờ, tách theo từng loại phương tiện (cho biểu đồ giờ cao điểm).</summary>
    public List<HourlyByVehicleTypeDto> HourlyByVehicleType { get; set; } = new();
}

public class HourlyByVehicleTypeDto
{
    public int Hour { get; set; }
    /// <summary>Map: tên loại xe -> số lượt vào trong giờ đó.</summary>
    public Dictionary<string, int> CountsByVehicleType { get; set; } = new();
}

public class FloorUtilizationDto
{
    public string FloorId { get; set; } = string.Empty;
    public string FloorName { get; set; } = string.Empty;
    public int FloorNumber { get; set; }
    public int TotalSlots { get; set; }
    public int OccupiedSlots { get; set; }
    public double OccupancyRate { get; set; }
}

public class ZoneUtilizationDto
{
    public string ZoneId { get; set; } = string.Empty;
    public string ZoneName { get; set; } = string.Empty;
    public string VehicleTypeId { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int CurrentOccupancy { get; set; }
    public double OccupancyRate { get; set; }
}

public class VehicleTypeUtilizationDto
{
    public string VehicleTypeId { get; set; } = string.Empty;
    public string VehicleTypeName { get; set; } = string.Empty;
    public int TotalSlots { get; set; }
    public int OccupiedSlots { get; set; }
    public double OccupancyRate { get; set; }
}

public class HourlyFlowDto
{
    public int Hour { get; set; }
    public int CheckIns { get; set; }
}
