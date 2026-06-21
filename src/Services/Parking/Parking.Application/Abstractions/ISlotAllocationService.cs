using Parking.Application.Common;
using Parking.Application.DTOs.Optimization;

namespace Parking.Application.Abstractions;

/// <summary>
/// Phân bổ slot đỗ tối ưu bằng thuật toán chấm điểm (deterministic):
/// kết hợp khoảng cách tới lối vào (proxy theo toạ độ lưới), cân bằng tỷ lệ
/// lấp đầy giữa các zone, và ưu tiên slot gọn khối.
/// </summary>
public interface ISlotAllocationService
{
    /// <summary>Gợi ý các slot tốt nhất (xếp theo điểm giảm dần) cho một zone + loại xe.</summary>
    Task<Result<List<SlotSuggestionDto>>> SuggestAsync(
        string zoneId,
        string vehicleTypeId,
        int topN,
        CancellationToken ct = default);

    /// <summary>
    /// Gợi ý slot bằng AI (Gemini): AI chọn chỗ tốt nhất từ các chỗ trống và giải thích lý do.
    /// Nếu AI lỗi sẽ tự động fallback về thuật toán chấm điểm.
    /// </summary>
    Task<Result<List<SlotSuggestionDto>>> SuggestWithAiAsync(
        string zoneId,
        string vehicleTypeId,
        int topN,
        CancellationToken ct = default);

    /// <summary>Chọn slot tốt nhất để tự động gán khi check-in. Trả về id slot.</summary>
    Task<Result<string>> PickBestSlotIdAsync(
        string zoneId,
        string vehicleTypeId,
        CancellationToken ct = default);
}
