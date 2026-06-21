namespace Report.Domain.Models;

public class VehicleFlowReport
{
    public DateTime From { get; set; }

    public DateTime To { get; set; }

    public int CheckIns { get; set; }

    public int CheckOuts { get; set; }

    // Phân bố lượt vào/ra theo từng giờ trong ngày (0-23, giờ địa phương GMT+7) —
    // dùng để xác định khung giờ cao điểm.
    public List<HourlyFlowBucket> PeakHours { get; set; } = new();

    // Thống kê lượt vào/ra theo từng loại phương tiện.
    public List<VehicleTypeFlow> ByVehicleType { get; set; } = new();
}

public class HourlyFlowBucket
{
    public int Hour { get; set; }   // 0-23

    public int CheckIns { get; set; }

    public int CheckOuts { get; set; }
}

public class VehicleTypeFlow
{
    public string VehicleTypeId { get; set; } = string.Empty;

    public int CheckIns { get; set; }

    public int CheckOuts { get; set; }
}
