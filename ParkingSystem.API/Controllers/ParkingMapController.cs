using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkingSystem.Application.Common;
using ParkingSystem.Application.DTOs;
using ParkingSystem.Application.Services;
using ParkingSystem.Domain.Constants;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ParkingSystem.API.Controllers;

[ApiController]
[Route("api/v1/parking-map")]
public class ParkingMapController : ControllerBase
{
    private readonly IParkingMapService _parkingMapService;

    public ParkingMapController(IParkingMapService parkingMapService)
    {
        _parkingMapService = parkingMapService;
    }

    [Authorize]
    [HttpGet("floors/{floorId}/map")]
    public async Task<ActionResult<ApiResponse<FloorMapDto>>> GetFloorMap(string floorId)
    {
        var map = await _parkingMapService.GetFloorMapAsync(floorId);
        if (map == null)
        {
            return NotFound(ApiResponse.Fail("Floor map not found."));
        }

        return Ok(ApiResponse.Ok("Floor map retrieved successfully", map));
    }

    [Authorize]
    [HttpGet("buildings/{buildingId}/floors")]
    public async Task<ActionResult<ApiResponse<IEnumerable<FloorDto>>>> GetFloorsByBuilding(string buildingId)
    {
        var floors = await _parkingMapService.GetFloorsByBuildingAsync(buildingId);
        return Ok(ApiResponse.Ok("Floors for building retrieved successfully", floors));
    }

    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.FacilityManager}")]
    [HttpPut("slots/{slotId}/position")]
    public async Task<ActionResult<ApiResponse>> UpdateSlotPosition(string slotId, [FromBody] UpdateSlotPositionDto dto)
    {
        try
        {
            var success = await _parkingMapService.UpdateSlotPositionAsync(slotId, dto);
            if (!success)
            {
                return NotFound(ApiResponse.Fail("Parking slot not found."));
            }

            return Ok(ApiResponse.Ok("Parking slot grid position updated successfully."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
    }

    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.FacilityManager}")]
    [HttpPost("floors/{floorId}/layout")]
    public async Task<ActionResult<ApiResponse>> GenerateGridLayout(string floorId, [FromBody] GenerateGridLayoutDto dto)
    {
        try
        {
            var success = await _parkingMapService.GenerateGridLayoutAsync(floorId, dto);
            if (!success)
            {
                return NotFound(ApiResponse.Fail("Floor not found."));
            }

            return Ok(ApiResponse.Ok("Chessboard parking slots grid layout generated successfully."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
    }
}
