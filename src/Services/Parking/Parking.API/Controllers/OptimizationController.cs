using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parking.Application.Abstractions;
using Parking.Application.Common;
using Parking.Application.DTOs;
using Parking.Application.DTOs.Optimization;

namespace Parking.API.Controllers;

[ApiController]
[Route("api/v1/optimization")]
[Authorize]
public class OptimizationController : ControllerBase
{
    private readonly IOptimizationService _optimization;
    private readonly ISlotAllocationService _allocation;

    public OptimizationController(
        IOptimizationService optimization,
        ISlotAllocationService allocation)
    {
        _optimization = optimization;
        _allocation = allocation;
    }

    /// <summary>Số liệu sử dụng bãi đỗ (không gọi AI). Dành cho Manager.</summary>
    [HttpGet("metrics")]
    [Authorize(Roles = "Admin,FacilityManager")]
    [ProducesResponseType(typeof(ApiResponse<OptimizationMetricsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMetrics([FromQuery] string buildingId, CancellationToken ct)
    {
        var result = await _optimization.GetMetricsAsync(buildingId, ct);
        if (!result.Success)
            return MapErrorMetrics(result);
        return Ok(ApiResponse<OptimizationMetricsDto>.Ok(result.Value!));
    }

    /// <summary>Số liệu + phân tích AI trả lời RQ1–RQ4. Dành cho Manager.</summary>
    [HttpPost("analyze")]
    [Authorize(Roles = "Admin,FacilityManager")]
    [ProducesResponseType(typeof(ApiResponse<OptimizationAnalysisDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Analyze([FromBody] AnalyzeRequest request, CancellationToken ct)
    {
        var result = await _optimization.AnalyzeAsync(request.BuildingId, ct);
        if (!result.Success)
            return MapErrorAnalysis(result);
        return Ok(ApiResponse<OptimizationAnalysisDto>.Ok(result.Value!));
    }

    /// <summary>Gợi ý slot tốt nhất cho một zone + loại xe. Dành cho cả nhân viên.</summary>
    [HttpGet("suggest-slot")]
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff")]
    [ProducesResponseType(typeof(ApiResponse<List<SlotSuggestionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SuggestSlot(
        [FromQuery] string zoneId,
        [FromQuery] string vehicleTypeId,
        [FromQuery] int topN = 5,
        CancellationToken ct = default)
    {
        var result = await _allocation.SuggestWithAiAsync(zoneId, vehicleTypeId, topN, ct);
        if (!result.Success)
        {
            var status = result.ErrorCode == ParkingErrorCodes.NoAvailableSlot
                ? StatusCodes.Status409Conflict
                : StatusCodes.Status400BadRequest;
            return StatusCode(status, ApiResponse.Fail(result.Error!));
        }
        return Ok(ApiResponse<List<SlotSuggestionDto>>.Ok(result.Value!));
    }

    private IActionResult MapErrorMetrics(Result<OptimizationMetricsDto> result)
    {
        var status = result.ErrorCode == ParkingErrorCodes.BuildingNotFound
            ? StatusCodes.Status404NotFound
            : StatusCodes.Status400BadRequest;
        return StatusCode(status, ApiResponse.Fail(result.Error!));
    }

    private IActionResult MapErrorAnalysis(Result<OptimizationAnalysisDto> result)
    {
        var status = result.ErrorCode == ParkingErrorCodes.BuildingNotFound
            ? StatusCodes.Status404NotFound
            : StatusCodes.Status400BadRequest;
        return StatusCode(status, ApiResponse.Fail(result.Error!));
    }

    public class AnalyzeRequest
    {
        public string BuildingId { get; set; } = string.Empty;
    }
}
