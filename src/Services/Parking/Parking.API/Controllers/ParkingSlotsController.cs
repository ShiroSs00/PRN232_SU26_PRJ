using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parking.Application.Abstractions;
using Parking.Application.Common;
using Parking.Application.DTOs;
using Parking.Application.DTOs.Slots;

namespace Parking.API.Controllers;

[ApiController]
[Route("api/v1/parking-slots")]
[Authorize]
public class ParkingSlotsController : ControllerBase
{
    private readonly IParkingSlotService _service;

    public ParkingSlotsController(IParkingSlotService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<SlotDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList(
        [FromQuery] string? buildingId,
        [FromQuery] string? floorId,
        [FromQuery] string? zoneId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _service.GetListAsync(buildingId, floorId, zoneId, page, pageSize, ct);
        return Ok(ApiResponse<PagedResult<SlotDto>>.Ok(result.Value!));
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff")]
    [ProducesResponseType(typeof(ApiResponse<SlotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        if (!result.Success)
            return NotFound(ApiResponse.Fail(result.Error!));
        return Ok(ApiResponse<SlotDto>.Ok(result.Value!));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,FacilityManager")]
    [ProducesResponseType(typeof(ApiResponse<SlotDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateSlotRequest request, CancellationToken ct)
    {
        var result = await _service.CreateAsync(request, ct);
        if (!result.Success)
            return StatusCode(MapErrorStatus(result.ErrorCode), ApiResponse.Fail(result.Error!));
        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value!.Id },
            ApiResponse<SlotDto>.Ok(result.Value!, "Parking slot created."));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,FacilityManager")]
    [ProducesResponseType(typeof(ApiResponse<SlotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateSlotRequest request, CancellationToken ct)
    {
        var result = await _service.UpdateAsync(id, request, ct);
        if (!result.Success)
            return StatusCode(MapErrorStatus(result.ErrorCode), ApiResponse.Fail(result.Error!));
        return Ok(ApiResponse<SlotDto>.Ok(result.Value!, "Parking slot updated."));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,FacilityManager")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await _service.DeleteAsync(id, ct);
        if (!result.Success)
            return StatusCode(MapErrorStatus(result.ErrorCode), ApiResponse.Fail(result.Error!));
        return Ok(ApiResponse.Ok("Parking slot deleted."));
    }

    [HttpPut("{id}/position")]
    [Authorize(Roles = "Admin,FacilityManager")]
    [ProducesResponseType(typeof(ApiResponse<SlotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdatePosition(string id, [FromBody] UpdateSlotPositionRequest request, CancellationToken ct)
    {
        var result = await _service.UpdatePositionAsync(id, request, ct);
        if (!result.Success)
            return StatusCode(MapErrorStatus(result.ErrorCode), ApiResponse.Fail(result.Error!));
        return Ok(ApiResponse<SlotDto>.Ok(result.Value!, "Slot position updated."));
    }

    [HttpPost("generate-grid")]
    [Authorize(Roles = "Admin,FacilityManager")]
    [ProducesResponseType(typeof(ApiResponse<List<SlotDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GenerateGrid([FromBody] GenerateGridRequest request, CancellationToken ct)
    {
        var result = await _service.GenerateGridAsync(request, ct);
        if (!result.Success)
            return StatusCode(MapErrorStatus(result.ErrorCode), ApiResponse.Fail(result.Error!));
        return Ok(ApiResponse<List<SlotDto>>.Ok(result.Value!, "Slot grid generated."));
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff")]
    [ProducesResponseType(typeof(ApiResponse<SlotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateSlotStatusRequest request, CancellationToken ct)
    {
        var result = await _service.UpdateStatusAsync(id, request, ct);
        if (!result.Success)
            return StatusCode(MapErrorStatus(result.ErrorCode), ApiResponse.Fail(result.Error!));
        return Ok(ApiResponse<SlotDto>.Ok(result.Value!, "Slot status updated."));
    }

    private static int MapErrorStatus(string? errorCode) => errorCode switch
    {
        ParkingErrorCodes.SlotNotFound => StatusCodes.Status404NotFound,
        ParkingErrorCodes.FloorNotFound => StatusCodes.Status404NotFound,
        ParkingErrorCodes.DuplicateSlotCode => StatusCodes.Status409Conflict,
        ParkingErrorCodes.SlotPositionTaken => StatusCodes.Status409Conflict,
        ParkingErrorCodes.SlotNotAvailable => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status400BadRequest
    };
}
