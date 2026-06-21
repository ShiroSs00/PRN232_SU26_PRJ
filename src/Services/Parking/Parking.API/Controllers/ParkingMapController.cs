using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parking.Application.Abstractions;
using Parking.Application.Common;
using Parking.Application.DTOs;
using Parking.Application.DTOs.Map;

namespace Parking.API.Controllers;

[ApiController]
[Route("api/v1/parking-map")]
[Authorize(Roles = "Admin,FacilityManager,ParkingStaff,Driver")]
public class ParkingMapController : ControllerBase
{
    private readonly IParkingMapService _service;

    public ParkingMapController(IParkingMapService service)
    {
        _service = service;
    }

    [HttpGet("floors/{floorId}/map")]
    [ProducesResponseType(typeof(ApiResponse<FloorMapDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFloorMap(string floorId, CancellationToken ct)
    {
        var result = await _service.GetFloorMapAsync(floorId, ct);
        if (!result.Success)
        {
            var status = result.ErrorCode == ParkingErrorCodes.FloorNotFound
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status400BadRequest;
            return StatusCode(status, ApiResponse.Fail(result.Error!));
        }
        return Ok(ApiResponse<FloorMapDto>.Ok(result.Value!));
    }

    [HttpGet("buildings/{buildingId}/floors")]
    [ProducesResponseType(typeof(ApiResponse<List<FloorOptionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFloorsByBuilding(string buildingId, CancellationToken ct)
    {
        var result = await _service.GetFloorsByBuildingAsync(buildingId, ct);
        if (!result.Success)
        {
            var status = result.ErrorCode == ParkingErrorCodes.BuildingNotFound
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status400BadRequest;
            return StatusCode(status, ApiResponse.Fail(result.Error!));
        }
        return Ok(ApiResponse<List<FloorOptionDto>>.Ok(result.Value!));
    }
}
