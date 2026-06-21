using Parking.Application.Common;
using Parking.Application.DTOs.Optimization;

namespace Parking.Application.Abstractions;

/// <summary>
/// Tổng hợp số liệu sử dụng bãi đỗ và sinh phân tích tối ưu (trả lời RQ1–RQ4) bằng AI.
/// </summary>
public interface IOptimizationService
{
    /// <summary>Số liệu lấp đầy theo tầng/zone/loại xe + lưu lượng theo giờ.</summary>
    Task<Result<OptimizationMetricsDto>> GetMetricsAsync(string buildingId, CancellationToken ct = default);

    /// <summary>Số liệu kèm phân tích AI (RQ1–RQ4). Nếu AI lỗi vẫn trả số liệu.</summary>
    Task<Result<OptimizationAnalysisDto>> AnalyzeAsync(string buildingId, CancellationToken ct = default);
}
