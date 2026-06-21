namespace Parking.Application.DTOs.Optimization;

/// <summary>
/// Kết quả phân tích tối ưu bãi đỗ: số liệu + nhận định do AI sinh ra (trả lời RQ1–RQ4).
/// </summary>
public class OptimizationAnalysisDto
{
    public OptimizationMetricsDto Metrics { get; set; } = new();

    /// <summary>Văn bản phân tích (markdown) do AI sinh, trả lời các câu hỏi nghiên cứu.</summary>
    public string Analysis { get; set; } = string.Empty;

    /// <summary>true nếu phần phân tích AI được tạo thành công; false nếu chỉ có số liệu.</summary>
    public bool AiAvailable { get; set; }
}
