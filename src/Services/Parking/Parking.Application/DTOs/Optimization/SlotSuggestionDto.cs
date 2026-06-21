namespace Parking.Application.DTOs.Optimization;

/// <summary>
/// Một slot được gợi ý kèm điểm số và lý do (deterministic, để giải thích cho người dùng).
/// </summary>
public class SlotSuggestionDto
{
    public string SlotId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Label { get; set; }
    public string ZoneId { get; set; } = string.Empty;
    public string FloorId { get; set; } = string.Empty;
    public int Row { get; set; }
    public int Column { get; set; }

    /// <summary>Điểm tổng hợp 0–100 (càng cao càng nên chọn).</summary>
    public double Score { get; set; }

    /// <summary>Lý do ngắn gọn vì sao slot này được ưu tiên.</summary>
    public string Reason { get; set; } = string.Empty;
}
