using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Report.Application.Abstractions;
using Report.Application.Common;
using Report.Application.DTOs;

namespace Report.API.Controllers;

[ApiController]
[Route("api/v1/reports")]
[Authorize(Roles = "Admin,FacilityManager")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reports;

    public ReportsController(IReportService reports)
    {
        _reports = reports;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        var result = await _reports.GetDashboardAsync(ct);
        return Ok(ApiResponse<object>.Ok(result.Value!));
    }

    [HttpGet("revenue")]
    public async Task<IActionResult> Revenue(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct = default)
    {
        var (f, t) = ResolveRange(from, to);
        var result = await _reports.GetRevenueAsync(f, t, ct);
        if (!result.Success)
            return BadRequest(ApiResponse.Fail(result.Error!));
        return Ok(ApiResponse<object>.Ok(result.Value!));
    }

    [HttpGet("occupancy")]
    public async Task<IActionResult> Occupancy(CancellationToken ct)
    {
        var result = await _reports.GetOccupancyAsync(ct);
        return Ok(ApiResponse<object>.Ok(result.Value!));
    }

    [HttpGet("vehicle-flow")]
    public async Task<IActionResult> VehicleFlow(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct = default)
    {
        var (f, t) = ResolveRange(from, to);
        var result = await _reports.GetVehicleFlowAsync(f, t, ct);
        if (!result.Success)
            return BadRequest(ApiResponse.Fail(result.Error!));
        return Ok(ApiResponse<object>.Ok(result.Value!));
    }

    [HttpGet("subscriptions")]
    public async Task<IActionResult> Subscriptions(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct = default)
    {
        var (f, t) = ResolveRange(from, to);
        var result = await _reports.GetSubscriptionsAsync(f, t, ct);
        if (!result.Success)
            return BadRequest(ApiResponse.Fail(result.Error!));
        return Ok(ApiResponse<object>.Ok(result.Value!));
    }

    [HttpGet("shift-reconciliation")]
    public async Task<IActionResult> ShiftReconciliation(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct = default)
    {
        var (f, t) = ResolveRange(from, to);
        var result = await _reports.GetShiftReconciliationAsync(f, t, ct);
        if (!result.Success)
            return BadRequest(ApiResponse.Fail(result.Error!));
        return Ok(ApiResponse<object>.Ok(result.Value!));
    }

    // Default range: last 30 days up to now if not supplied.
    private static (DateTime From, DateTime To) ResolveRange(DateTime? from, DateTime? to)
    {
        var t = to ?? DateTime.UtcNow;
        var f = from ?? t.AddDays(-30);
        return (f, t);
    }
}
