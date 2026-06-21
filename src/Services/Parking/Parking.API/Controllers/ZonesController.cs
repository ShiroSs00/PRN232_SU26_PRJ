using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parking.Application.Abstractions;
using Parking.Application.Common;
using Parking.Application.DTOs;
using Parking.Application.DTOs.MasterData;

namespace Parking.API.Controllers;

[ApiController]
[Route("api/v1/zones")]
[Authorize]
public class ZonesController : ControllerBase
{
    private readonly IZoneService _service;

    public ZonesController(IZoneService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ZoneDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList(
        [FromQuery] string? buildingId,
        [FromQuery] string? floorId,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _service.GetListAsync(buildingId, floorId, isActive, page, pageSize, ct);
        return Ok(ApiResponse<PagedResult<ZoneDto>>.Ok(result.Value!));
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff")]
    [ProducesResponseType(typeof(ApiResponse<ZoneDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        if (!result.Success)
            return NotFound(ApiResponse.Fail(result.Error!));
        return Ok(ApiResponse<ZoneDto>.Ok(result.Value!));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,FacilityManager")]
    [ProducesResponseType(typeof(ApiResponse<ZoneDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateZoneRequest request,
        CancellationToken ct)
    {
        var result = await _service.CreateAsync(request, ct);
        if (!result.Success)
            return BadRequest(ApiResponse.Fail(result.Error!));
        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value!.Id },
            ApiResponse<ZoneDto>.Ok(result.Value!, "Zone created."));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,FacilityManager")]
    [ProducesResponseType(typeof(ApiResponse<ZoneDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        string id,
        [FromBody] UpdateZoneRequest request,
        CancellationToken ct)
    {
        var result = await _service.UpdateAsync(id, request, ct);
        if (!result.Success)
        {
            var status = result.ErrorCode switch
            {
                ParkingErrorCodes.ZoneNotFound => StatusCodes.Status404NotFound,
                ParkingErrorCodes.VehicleTypeNotFound => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status400BadRequest
            };
            return StatusCode(status, ApiResponse.Fail(result.Error!));
        }
        return Ok(ApiResponse<ZoneDto>.Ok(result.Value!, "Zone updated."));
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
        return Ok(ApiResponse.Ok("Zone deactivated."));
    }
}
