using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parking.Application.Abstractions;
using Parking.Application.Common;
using Parking.Application.DTOs;
using Parking.Application.DTOs.Shifts;

namespace Parking.API.Controllers;

[ApiController]
[Route("api/v1/shifts")]
[Authorize(Roles = "Admin,FacilityManager,ParkingStaff")]
public sealed class ShiftsController : ControllerBase
{
    private readonly IShiftService _service;

    public ShiftsController(IShiftService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ShiftDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetList([FromQuery] ShiftListQuery query, CancellationToken ct)
    {
        var result = await _service.GetListAsync(query, ct);
        if (!result.Success)
            return BadRequest(ApiResponse.Fail(result.Error!));
        return Ok(ApiResponse<PagedResult<ShiftDto>>.Ok(result.Value!));
    }

    [HttpGet("current")]
    [ProducesResponseType(typeof(ApiResponse<ShiftDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrent(CancellationToken ct)
    {
        var result = await _service.GetCurrentAsync(GetUserId(), ct);
        if (!result.Success)
            return NotFound(ApiResponse.Fail(result.Error!));
        return Ok(ApiResponse<ShiftDto>.Ok(result.Value!));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ShiftDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        if (!result.Success)
            return NotFound(ApiResponse.Fail(result.Error!));
        return Ok(ApiResponse<ShiftDto>.Ok(result.Value!));
    }

    [HttpPost("open")]
    [ProducesResponseType(typeof(ApiResponse<ShiftDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Open([FromBody] OpenShiftRequest request, CancellationToken ct)
    {
        var result = await _service.OpenAsync(GetUserId(), request, ct);
        if (!result.Success)
            return MapError(result);
        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value!.Id },
            ApiResponse<ShiftDto>.Ok(result.Value!, "Shift opened."));
    }

    [HttpPost("{id}/close")]
    [ProducesResponseType(typeof(ApiResponse<ShiftDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Close(
        string id,
        [FromBody] CloseShiftRequest request,
        CancellationToken ct)
    {
        var canManageAll = User.IsInRole("Admin") || User.IsInRole("FacilityManager");
        var result = await _service.CloseAsync(id, GetUserId(), canManageAll, request, ct);
        if (!result.Success)
            return MapError(result);
        return Ok(ApiResponse<ShiftDto>.Ok(result.Value!, "Shift closed."));
    }

    private IActionResult MapError(Result<ShiftDto> result)
    {
        var status = result.ErrorCode switch
        {
            ParkingErrorCodes.BuildingNotFound => StatusCodes.Status404NotFound,
            ParkingErrorCodes.ShiftNotFound => StatusCodes.Status404NotFound,
            ParkingErrorCodes.ShiftAccessDenied => StatusCodes.Status403Forbidden,
            ParkingErrorCodes.OpenShiftAlreadyExists => StatusCodes.Status409Conflict,
            ParkingErrorCodes.ShiftNotOpen => StatusCodes.Status409Conflict,
            ParkingErrorCodes.ShiftHasPendingPayments => StatusCodes.Status409Conflict,
            ParkingErrorCodes.PaymentServiceUnavailable => StatusCodes.Status502BadGateway,
            _ => StatusCodes.Status400BadRequest
        };
        return StatusCode(status, ApiResponse.Fail(result.Error!));
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ??
        User.FindFirstValue("sub") ??
        string.Empty;
}
