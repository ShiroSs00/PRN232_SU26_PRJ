using System.Text;
using System.Text.Json;
using MongoDB.Driver;
using Parking.Application.Abstractions;
using Parking.Application.Common;
using Parking.Application.DTOs.Optimization;
using Parking.Domain.Entities;
using Parking.Domain.Enums;
using Parking.Infrastructure.Persistence;

namespace Parking.Infrastructure.Services;

/// <summary>
/// Phân bổ slot bằng thuật toán chấm điểm deterministic.
/// Điểm tổng hợp 0–100 = trọng số * các tiêu chí đã chuẩn hoá [0,1]:
///   - Gần lối vào (proxy theo toạ độ lưới row+column): trọng số 0.5
///   - Cân bằng lấp đầy zone (zone càng trống càng ưu tiên):  trọng số 0.3
///   - Gọn khối (slot 1x1, không phá vỡ ô lớn):               trọng số 0.2
/// Riêng gợi ý cho nhân viên có thể dùng AI (Gemini) để chọn + giải thích.
/// </summary>
public class SlotAllocationService : ISlotAllocationService
{
    private readonly MongoDbContext _db;
    private readonly IGeminiClient _gemini;

    private const double WeightProximity = 0.5;
    private const double WeightZoneBalance = 0.3;
    private const double WeightCompact = 0.2;

    public SlotAllocationService(MongoDbContext db, IGeminiClient gemini)
    {
        _db = db;
        _gemini = gemini;
    }

    public async Task<Result<List<SlotSuggestionDto>>> SuggestAsync(
        string zoneId, string vehicleTypeId, int topN, CancellationToken ct = default)
    {
        var ranked = await RankAsync(zoneId, vehicleTypeId, ct);
        if (ranked.Count == 0)
            return Result<List<SlotSuggestionDto>>.Fail(
                "Không còn chỗ trống trong khu vực này.", ParkingErrorCodes.NoAvailableSlot);

        var top = ranked.Take(topN <= 0 ? 5 : topN).Select(x => x.Dto).ToList();
        return Result<List<SlotSuggestionDto>>.Ok(top);
    }

    public async Task<Result<List<SlotSuggestionDto>>> SuggestWithAiAsync(
        string zoneId, string vehicleTypeId, int topN, CancellationToken ct = default)
    {
        var ranked = await RankAsync(zoneId, vehicleTypeId, ct);
        if (ranked.Count == 0)
            return Result<List<SlotSuggestionDto>>.Fail(
                "Không còn chỗ trống trong khu vực này.", ParkingErrorCodes.NoAvailableSlot);

        var candidates = ranked.Select(x => x.Dto).ToList();
        var n = topN <= 0 ? 5 : topN;

        // Nhờ Gemini chọn chỗ tốt nhất từ danh sách chỗ trống và viết lý do.
        var prompt = BuildSuggestPrompt(candidates);
        var aiResult = await _gemini.GenerateAsync(prompt, ct);

        if (aiResult.Success)
        {
            var aiPick = ParseAiPick(aiResult.Value!, candidates);
            if (aiPick != null)
            {
                // Đưa chỗ AI chọn lên đầu, kèm các lựa chọn thuật toán còn lại.
                var rest = candidates.Where(c => c.SlotId != aiPick.SlotId).Take(n - 1);
                var list = new List<SlotSuggestionDto> { aiPick };
                list.AddRange(rest);
                return Result<List<SlotSuggestionDto>>.Ok(list);
            }
        }

        // Fallback: AI lỗi hoặc trả về không hợp lệ → dùng kết quả thuật toán.
        return Result<List<SlotSuggestionDto>>.Ok(candidates.Take(n).ToList());
    }

    public async Task<Result<string>> PickBestSlotIdAsync(
        string zoneId, string vehicleTypeId, CancellationToken ct = default)
    {
        var ranked = await RankAsync(zoneId, vehicleTypeId, ct);
        if (ranked.Count == 0)
            return Result<string>.Fail(
                "Không còn chỗ trống trong khu vực này.", ParkingErrorCodes.NoAvailableSlot);

        return Result<string>.Ok(ranked[0].Dto.SlotId);
    }

    /// <summary>Chấm điểm và xếp hạng toàn bộ slot Available của zone+loại xe.</summary>
    private async Task<List<(SlotSuggestionDto Dto, ParkingSlot Slot)>> RankAsync(
        string zoneId, string vehicleTypeId, CancellationToken ct)
    {
        var slots = await _db.ParkingSlots
            .Find(x => x.ZoneId == zoneId
                && x.VehicleTypeId == vehicleTypeId
                && x.Status == SlotStatus.Available
                && x.IsActive)
            .ToListAsync(ct);

        if (slots.Count == 0)
            return new List<(SlotSuggestionDto, ParkingSlot)>();

        // Dữ liệu phụ trợ để chuẩn hoá.
        var zone = await _db.Zones.Find(z => z.Id == zoneId).FirstOrDefaultAsync(ct);
        var floorId = slots[0].FloorId;
        var floor = await _db.Floors.Find(f => f.Id == floorId).FirstOrDefaultAsync(ct);

        // Kích thước lưới để chuẩn hoá khoảng cách. Phòng trường hợp thiếu dữ liệu.
        int gridRows = floor?.GridRows > 0 ? floor.GridRows : Math.Max(1, slots.Max(s => s.Row));
        int gridCols = floor?.GridCols > 0 ? floor.GridCols : Math.Max(1, slots.Max(s => s.Column));
        double maxDist = Math.Max(1, gridRows + gridCols);

        // Tỷ lệ trống của zone (zone càng trống điểm cân bằng càng cao).
        double zoneVacancy = 1.0;
        if (zone is not null && zone.Capacity > 0)
            zoneVacancy = Math.Clamp(1.0 - (double)zone.CurrentOccupancy / zone.Capacity, 0, 1);

        var ranked = new List<(SlotSuggestionDto Dto, ParkingSlot Slot)>();

        foreach (var s in slots)
        {
            // 1) Gần lối vào: row+column nhỏ → gần gốc lưới → điểm cao.
            double proximity = Math.Clamp(1.0 - (double)(s.Row + s.Column) / maxDist, 0, 1);

            // 2) Cân bằng lấp đầy: dùng độ trống của zone (giống nhau trong cùng zone,
            //    nhưng giữ công thức để mở rộng đa-zone về sau).
            double zoneBalance = zoneVacancy;

            // 3) Gọn khối: slot 1x1 được ưu tiên tối đa.
            bool compact = (s.RowSpan <= 1 && s.ColSpan <= 1);
            double compactScore = compact ? 1.0 : 0.5;

            double score = (WeightProximity * proximity
                + WeightZoneBalance * zoneBalance
                + WeightCompact * compactScore) * 100.0;

            var reasons = new List<string>();
            if (proximity >= 0.66) reasons.Add("gần lối vào");
            else if (proximity >= 0.33) reasons.Add("vị trí trung bình");
            else reasons.Add("xa lối vào");
            if (zoneBalance >= 0.5) reasons.Add("khu vực còn nhiều chỗ");
            if (compact) reasons.Add("ô tiêu chuẩn");

            ranked.Add((new SlotSuggestionDto
            {
                SlotId = s.Id,
                Code = s.Code,
                Label = s.Label,
                ZoneId = s.ZoneId,
                FloorId = s.FloorId,
                Row = s.Row,
                Column = s.Column,
                Score = Math.Round(score, 1),
                Reason = string.Join(", ", reasons),
            }, s));
        }

        // Điểm cao trước; hoà thì theo Row/Column để ổn định, dễ kiểm thử.
        return ranked
            .OrderByDescending(x => x.Dto.Score)
            .ThenBy(x => x.Slot.Row)
            .ThenBy(x => x.Slot.Column)
            .ToList();
    }

    /// <summary>Dựng prompt cho Gemini: chọn 1 chỗ tốt nhất từ danh sách + giải thích.</summary>
    private static string BuildSuggestPrompt(List<SlotSuggestionDto> candidates)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Bạn là hệ thống điều phối bãi đỗ xe. Hãy chọn MỘT chỗ đỗ tốt nhất cho xe sắp vào, ưu tiên giảm thời gian tìm chỗ (gần lối vào) và phân bổ hợp lý.");
        sb.AppendLine("Danh sách chỗ trống (code | row | column | điểm hệ thống):");
        foreach (var c in candidates)
            sb.AppendLine($"- {c.Code} | row {c.Row} | col {c.Column} | {c.Score}");
        sb.AppendLine();
        sb.AppendLine("Chỉ trả về JSON đúng định dạng, không thêm chữ nào khác:");
        sb.AppendLine("{\"code\":\"<mã chỗ đã chọn>\",\"reason\":\"<lý do ngắn gọn bằng tiếng Việt, tối đa 15 từ>\"}");
        return sb.ToString();
    }

    /// <summary>Phân tích phản hồi JSON của Gemini, tìm slot tương ứng trong danh sách.</summary>
    private static SlotSuggestionDto? ParseAiPick(string aiText, List<SlotSuggestionDto> candidates)
    {
        try
        {
            // Cắt phần JSON (Gemini đôi khi bọc trong ```json ... ```).
            var start = aiText.IndexOf('{');
            var end = aiText.LastIndexOf('}');
            if (start < 0 || end <= start) return null;

            var json = aiText.Substring(start, end - start + 1);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("code", out var codeEl)) return null;
            var code = codeEl.GetString();
            if (string.IsNullOrWhiteSpace(code)) return null;

            var match = candidates.FirstOrDefault(
                c => string.Equals(c.Code, code.Trim(), StringComparison.OrdinalIgnoreCase));
            if (match == null) return null;

            var reason = root.TryGetProperty("reason", out var reasonEl)
                ? reasonEl.GetString()
                : null;

            // Trả về bản sao với lý do do AI viết (đánh dấu nguồn AI).
            return new SlotSuggestionDto
            {
                SlotId = match.SlotId,
                Code = match.Code,
                Label = match.Label,
                ZoneId = match.ZoneId,
                FloorId = match.FloorId,
                Row = match.Row,
                Column = match.Column,
                Score = match.Score,
                Reason = string.IsNullOrWhiteSpace(reason)
                    ? $"AI đề xuất ({match.Reason})"
                    : $"AI: {reason!.Trim()}",
            };
        }
        catch
        {
            return null;
        }
    }
}
