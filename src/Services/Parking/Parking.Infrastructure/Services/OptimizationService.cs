using System.Text;
using MongoDB.Driver;
using Parking.Application.Abstractions;
using Parking.Application.Common;
using Parking.Application.DTOs.Optimization;
using Parking.Domain.Entities;
using Parking.Domain.Enums;
using Parking.Infrastructure.Persistence;

namespace Parking.Infrastructure.Services;

/// <summary>
/// Tổng hợp số liệu sử dụng bãi đỗ theo tầng/zone/loại xe và lưu lượng theo giờ,
/// sau đó dựng prompt trả lời RQ1–RQ4 và gọi Gemini sinh phân tích.
/// </summary>
public class OptimizationService : IOptimizationService
{
    private readonly MongoDbContext _db;
    private readonly IGeminiClient _gemini;

    public OptimizationService(MongoDbContext db, IGeminiClient gemini)
    {
        _db = db;
        _gemini = gemini;
    }

    public async Task<Result<OptimizationMetricsDto>> GetMetricsAsync(string buildingId, CancellationToken ct = default)
    {
        var building = await _db.Buildings.Find(b => b.Id == buildingId).FirstOrDefaultAsync(ct);
        if (building is null)
            return Result<OptimizationMetricsDto>.Fail("Không tìm thấy tòa nhà.", ParkingErrorCodes.BuildingNotFound);

        var metrics = await BuildMetricsAsync(building, ct);
        return Result<OptimizationMetricsDto>.Ok(metrics);
    }

    public async Task<Result<OptimizationAnalysisDto>> AnalyzeAsync(string buildingId, CancellationToken ct = default)
    {
        var building = await _db.Buildings.Find(b => b.Id == buildingId).FirstOrDefaultAsync(ct);
        if (building is null)
            return Result<OptimizationAnalysisDto>.Fail("Không tìm thấy tòa nhà.", ParkingErrorCodes.BuildingNotFound);

        var metrics = await BuildMetricsAsync(building, ct);
        var prompt = BuildPrompt(metrics);

        var aiResult = await _gemini.GenerateAsync(prompt, ct);

        var dto = new OptimizationAnalysisDto
        {
            Metrics = metrics,
            AiAvailable = aiResult.Success,
            Analysis = aiResult.Success
                ? aiResult.Value!
                : "Không tạo được phân tích AI (" + aiResult.Error + "). Vui lòng kiểm tra cấu hình Gemini.",
        };
        return Result<OptimizationAnalysisDto>.Ok(dto);
    }

    private async Task<OptimizationMetricsDto> BuildMetricsAsync(Building building, CancellationToken ct)
    {
        var slots = await _db.ParkingSlots
            .Find(s => s.BuildingId == building.Id && s.IsActive)
            .ToListAsync(ct);
        var floors = await _db.Floors
            .Find(f => f.BuildingId == building.Id && f.IsActive)
            .ToListAsync(ct);
        var zones = await _db.Zones
            .Find(z => z.BuildingId == building.Id && z.IsActive)
            .ToListAsync(ct);
        var vehicleTypes = await _db.VehicleTypes
            .Find(v => v.IsActive)
            .ToListAsync(ct);

        var activeSessions = await _db.ParkingSessions
            .Find(s => s.BuildingId == building.Id && s.Status == ParkingSessionStatus.Active)
            .ToListAsync(ct);

        // Lưu lượng xe vào theo giờ (toàn bộ phiên của tòa nhà).
        var allSessions = await _db.ParkingSessions
            .Find(s => s.BuildingId == building.Id)
            .ToListAsync(ct);

        int Count(SlotStatus st) => slots.Count(s => s.Status == st);
        int total = slots.Count;
        int occupied = Count(SlotStatus.Occupied);

        var floorName = floors.ToDictionary(f => f.Id, f => f);
        var vtName = vehicleTypes.ToDictionary(v => v.Id, v => v.Name);

        var metrics = new OptimizationMetricsDto
        {
            BuildingId = building.Id,
            BuildingName = building.Name,
            TotalSlots = total,
            AvailableSlots = Count(SlotStatus.Available),
            OccupiedSlots = occupied,
            ReservedSlots = Count(SlotStatus.Reserved),
            MaintenanceSlots = Count(SlotStatus.Maintenance),
            OccupancyRate = total > 0 ? Math.Round((double)occupied / total * 100, 1) : 0,
            ActiveSessions = activeSessions.Count,
        };

        // Theo tầng.
        foreach (var fg in slots.GroupBy(s => s.FloorId))
        {
            int ft = fg.Count();
            int fo = fg.Count(s => s.Status == SlotStatus.Occupied);
            floorName.TryGetValue(fg.Key, out var fl);
            metrics.Floors.Add(new FloorUtilizationDto
            {
                FloorId = fg.Key,
                FloorName = fl?.Name ?? fg.Key,
                FloorNumber = fl?.FloorNumber ?? 0,
                TotalSlots = ft,
                OccupiedSlots = fo,
                OccupancyRate = ft > 0 ? Math.Round((double)fo / ft * 100, 1) : 0,
            });
        }
        metrics.Floors = metrics.Floors.OrderBy(f => f.FloorNumber).ToList();

        // Theo zone.
        foreach (var z in zones)
        {
            metrics.Zones.Add(new ZoneUtilizationDto
            {
                ZoneId = z.Id,
                ZoneName = z.Name,
                VehicleTypeId = z.VehicleTypeId,
                Capacity = z.Capacity,
                CurrentOccupancy = z.CurrentOccupancy,
                OccupancyRate = z.Capacity > 0
                    ? Math.Round((double)z.CurrentOccupancy / z.Capacity * 100, 1) : 0,
            });
        }

        // Theo loại xe.
        foreach (var vg in slots.GroupBy(s => s.VehicleTypeId))
        {
            int vt = vg.Count();
            int vo = vg.Count(s => s.Status == SlotStatus.Occupied);
            metrics.VehicleTypes.Add(new VehicleTypeUtilizationDto
            {
                VehicleTypeId = vg.Key,
                VehicleTypeName = vtName.TryGetValue(vg.Key, out var n) ? n : vg.Key,
                TotalSlots = vt,
                OccupiedSlots = vo,
                OccupancyRate = vt > 0 ? Math.Round((double)vo / vt * 100, 1) : 0,
            });
        }

        // Lưu lượng theo giờ (0–23).
        var byHour = allSessions
            .GroupBy(s => s.CheckInTime.Hour)
            .ToDictionary(g => g.Key, g => g.Count());
        for (int h = 0; h < 24; h++)
        {
            metrics.HourlyCheckIns.Add(new HourlyFlowDto
            {
                Hour = h,
                CheckIns = byHour.TryGetValue(h, out var c) ? c : 0,
            });
        }

        // Lưu lượng theo giờ tách theo loại phương tiện (cho biểu đồ giờ cao điểm).
        var typeNames = allSessions
            .Select(s => s.VehicleTypeId)
            .Distinct()
            .ToDictionary(id => id, id => vtName.TryGetValue(id, out var n) ? n : id);
        for (int h = 0; h < 24; h++)
        {
            var counts = allSessions
                .Where(s => s.CheckInTime.Hour == h)
                .GroupBy(s => typeNames.TryGetValue(s.VehicleTypeId, out var n) ? n : s.VehicleTypeId)
                .ToDictionary(g => g.Key, g => g.Count());
            metrics.HourlyByVehicleType.Add(new HourlyByVehicleTypeDto
            {
                Hour = h,
                CountsByVehicleType = counts,
            });
        }

        return metrics;
    }

    /// <summary>Dựng prompt tiếng Việt, nhúng số liệu thật, yêu cầu trả lời RQ1–RQ4.</summary>
    private static string BuildPrompt(OptimizationMetricsDto m)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Bạn là chuyên gia tối ưu vận hành bãi đỗ xe thông minh. Dưới đây là số liệu thực tế của một tòa nhà gửi xe. Hãy phân tích NGẮN GỌN, bám sát số liệu, và trả lời lần lượt 4 câu hỏi nghiên cứu. Trình bày bằng tiếng Việt, dùng markdown với tiêu đề cho mỗi RQ.");
        sb.AppendLine();
        sb.AppendLine($"## Số liệu tòa nhà: {m.BuildingName}");
        sb.AppendLine($"- Tổng slot: {m.TotalSlots} | Trống: {m.AvailableSlots} | Đang đỗ: {m.OccupiedSlots} | Đã đặt: {m.ReservedSlots} | Bảo trì: {m.MaintenanceSlots}");
        sb.AppendLine($"- Tỷ lệ lấp đầy toàn tòa: {m.OccupancyRate}% | Phiên đang hoạt động: {m.ActiveSessions}");

        sb.AppendLine();
        sb.AppendLine("### Theo tầng:");
        foreach (var f in m.Floors)
            sb.AppendLine($"- {f.FloorName} (tầng {f.FloorNumber}): {f.OccupiedSlots}/{f.TotalSlots} đang dùng ({f.OccupancyRate}%)");

        sb.AppendLine();
        sb.AppendLine("### Theo khu vực (zone):");
        foreach (var z in m.Zones)
            sb.AppendLine($"- {z.ZoneName}: {z.CurrentOccupancy}/{z.Capacity} ({z.OccupancyRate}%)");

        sb.AppendLine();
        sb.AppendLine("### Theo loại phương tiện:");
        foreach (var v in m.VehicleTypes)
            sb.AppendLine($"- {v.VehicleTypeName}: {v.OccupiedSlots}/{v.TotalSlots} đang dùng ({v.OccupancyRate}%)");

        sb.AppendLine();
        sb.AppendLine("### Lưu lượng xe vào theo giờ (giờ: số lượt):");
        var peak = m.HourlyCheckIns.Where(h => h.CheckIns > 0).ToList();
        if (peak.Count == 0)
            sb.AppendLine("- Chưa có dữ liệu lượt vào.");
        else
            sb.AppendLine("- " + string.Join(", ", peak.Select(h => $"{h.Hour}h:{h.CheckIns}")));

        sb.AppendLine();
        sb.AppendLine("### Giờ cao điểm theo loại phương tiện (giờ: loại=lượt):");
        var peakByType = m.HourlyByVehicleType.Where(h => h.CountsByVehicleType.Count > 0).ToList();
        if (peakByType.Count == 0)
            sb.AppendLine("- Chưa có dữ liệu.");
        else
            foreach (var h in peakByType)
                sb.AppendLine($"- {h.Hour}h: " + string.Join(", ", h.CountsByVehicleType.Select(kv => $"{kv.Key}={kv.Value}")));

        sb.AppendLine();
        sb.AppendLine("## Trả lời các câu hỏi nghiên cứu:");
        sb.AppendLine("RQ1: Việc phân tầng, khu vực theo loại phương tiện ảnh hưởng thế nào đến hiệu quả sử dụng chỗ đỗ (dựa trên số liệu trên)?");
        sb.AppendLine("RQ2: Phân bổ slot tự động có giúp giảm thời gian tìm chỗ so với cách chọn chỗ tự do không?");
        sb.AppendLine("RQ3: Nên ưu tiên tiêu chí nào khi phân bổ slot (khoảng cách, tầng, loại xe, thời gian gửi, tỷ lệ lấp đầy)?");
        sb.AppendLine("RQ4: Thuật toán phân bổ slot có thể cải thiện tỷ lệ sử dụng bãi xe trong giờ cao điểm không?");
        sb.AppendLine();
        sb.AppendLine("Cuối cùng, đưa ra 2-3 khuyến nghị hành động cụ thể cho ban quản lý dựa trên số liệu.");
        return sb.ToString();
    }
}
