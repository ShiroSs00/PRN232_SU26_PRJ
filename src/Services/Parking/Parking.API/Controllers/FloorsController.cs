using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parking.Application.Abstractions;
using Parking.Application.Common;
using Parking.Application.DTOs;
using Parking.Application.DTOs.MasterData;

namespace Parking.API.Controllers;

[ApiController]
[Route("api/v1/floors")]
[Authorize]
public class FloorsController : ControllerBase
{
    private readonly IFloorService _service;

    public FloorsController(IFloorService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<FloorDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList(
        [FromQuery] string? buildingId,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _service.GetListAsync(buildingId, isActive, page, pageSize, ct);
        return Ok(ApiResponse<PagedResult<FloorDto>>.Ok(result.Value!));
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff")]
    [ProducesResponseType(typeof(ApiResponse<FloorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        if (!result.Success)
            return NotFound(ApiResponse.Fail(result.Error!));
        return Ok(ApiResponse<FloorDto>.Ok(result.Value!));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,FacilityManager")]
    [ProducesResponseType(typeof(ApiResponse<FloorDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateFloorRequest request,
        CancellationToken ct)
    {
        var result = await _service.CreateAsync(request, ct);
        if (!result.Success)
            return BadRequest(ApiResponse.Fail(result.Error!));
        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value!.Id },
            ApiResponse<FloorDto>.Ok(result.Value!, "Floor created."));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,FacilityManager")]
    [ProducesResponseType(typeof(ApiResponse<FloorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        string id,
        [FromBody] UpdateFloorRequest request,
        CancellationToken ct)
    {
        var result = await _service.UpdateAsync(id, request, ct);
        if (!result.Success)
        {
            var status = result.ErrorCode == ParkingErrorCodes.FloorNotFound
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status400BadRequest;
            return StatusCode(status, ApiResponse.Fail(result.Error!));
        }
        return Ok(ApiResponse<FloorDto>.Ok(result.Value!, "Floor updated."));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,FacilityManager")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await _service.DeleteAsync(id, ct);
        if (!result.Success)
            return NotFound(ApiResponse.Fail(result.Error!));
        return Ok(ApiResponse.Ok("Floor deactivated."));
    }
}
