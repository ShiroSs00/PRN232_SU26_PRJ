using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parking.Application.Abstractions;
using Parking.Application.Common;
using Parking.Application.DTOs;
using Parking.Application.DTOs.Incidents;

namespace Parking.API.Controllers;

[ApiController]
[Route("api/v1/incidents")]
[Authorize]
public class IncidentsController : ControllerBase
{
    private readonly IIncidentService _service;

    public IncidentsController(IIncidentService service)
    {
        _service = service;
    }

    // Danh sách sự cố (lọc theo tòa nhà/trạng thái/loại/biển số/xe/phiên).
    [HttpGet]
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<IncidentReportDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList([FromQuery] IncidentListQuery query, CancellationToken ct)
    {
        var result = await _service.GetListAsync(query, ct);
        return Ok(ApiResponse<PagedResult<IncidentReportDto>>.Ok(result.Value!));
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff")]
    [ProducesResponseType(typeof(ApiResponse<IncidentReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        if (!result.Success)
            return NotFound(ApiResponse.Fail(result.Error!));
        return Ok(ApiResponse<IncidentReportDto>.Ok(result.Value!));
    }

    // Nhân viên/quản lý ghi nhận sự cố.
    [HttpPost]
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff")]
    [ProducesResponseType(typeof(ApiResponse<IncidentReportDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateIncidentRequest request, CancellationToken ct)
    {
        var result = await _service.CreateAsync(request, GetUserId(), ct);
        if (!result.Success)
            return BadRequest(ApiResponse.Fail(result.Error!));
        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value!.Id },
            ApiResponse<IncidentReportDto>.Ok(result.Value!, "Đã ghi nhận sự cố."));
    }

    // Quản lý cập nhật nội dung/loại/trạng thái sự cố.
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,FacilityManager")]
    [ProducesResponseType(typeof(ApiResponse<IncidentReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateIncidentRequest request, CancellationToken ct)
    {
        var result = await _service.UpdateAsync(id, request, ct);
        if (!result.Success)
            return Translate(result.ErrorCode, result.Error!);
        return Ok(ApiResponse<IncidentReportDto>.Ok(result.Value!, "Đã cập nhật sự cố."));
    }

    // Quản lý xử lý/đóng sự cố (kèm ghi chú xử lý).
    [HttpPost("{id}/resolve")]
    [Authorize(Roles = "Admin,FacilityManager")]
    [ProducesResponseType(typeof(ApiResponse<IncidentReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Resolve(string id, [FromBody] ResolveIncidentRequest request, CancellationToken ct)
    {
        var result = await _service.ResolveAsync(id, request, GetUserId(), ct);
        if (!result.Success)
            return Translate(result.ErrorCode, result.Error!);
        return Ok(ApiResponse<IncidentReportDto>.Ok(result.Value!, "Đã xử lý sự cố."));
    }

    // Quản lý hủy sự cố (báo nhầm/không hợp lệ).
    [HttpPost("{id}/cancel")]
    [Authorize(Roles = "Admin,FacilityManager")]
    [ProducesResponseType(typeof(ApiResponse<IncidentReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(string id, CancellationToken ct)
    {
        var result = await _service.CancelAsync(id, ct);
        if (!result.Success)
            return Translate(result.ErrorCode, result.Error!);
        return Ok(ApiResponse<IncidentReportDto>.Ok(result.Value!, "Đã hủy sự cố."));
    }

    private IActionResult Translate(string? errorCode, string error)
    {
        var status = errorCode == ParkingErrorCodes.IncidentNotFound
            ? StatusCodes.Status404NotFound
            : StatusCodes.Status400BadRequest;
        return StatusCode(status, ApiResponse.Fail(error));
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ??
        User.FindFirstValue("sub") ??
        string.Empty;
}
