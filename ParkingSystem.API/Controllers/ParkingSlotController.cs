using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkingSystem.Application.Common;
using ParkingSystem.Application.DTOs;
using ParkingSystem.Application.Services;
using ParkingSystem.Domain.Constants;

namespace ParkingSystem.API.Controllers;

[ApiController]
[Route("api/v1/parking-slots")]
public class ParkingSlotController : ControllerBase
{
    private readonly IParkingSlotService _parkingSlotService;

    public ParkingSlotController(IParkingSlotService parkingSlotService)
    {
        _parkingSlotService = parkingSlotService;
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<ParkingSlotDto>>>> GetAll(
        [FromQuery] string? buildingId,
        [FromQuery] string? floorId,
        [FromQuery] string? zoneId,
        [FromQuery] string? vehicleTypeId,
        [FromQuery] string? status)
    {
        var slots = await _parkingSlotService.GetAllAsync(buildingId, floorId, zoneId, vehicleTypeId, status);
        return Ok(ApiResponse.Ok("Parking slots retrieved successfully", slots));
    }

    [Authorize]
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ParkingSlotDto>>> GetById(string id)
    {
        var slot = await _parkingSlotService.GetByIdAsync(id);
        if (slot == null)
        {
            return NotFound(ApiResponse.Fail("Parking slot not found."));
        }

        return Ok(ApiResponse.Ok("Parking slot retrieved successfully", slot));
    }

    [Authorize]
    [HttpGet("by-zone/{zoneId}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ParkingSlotDto>>>> GetByZoneId(string zoneId)
    {
        var slots = await _parkingSlotService.GetByZoneIdAsync(zoneId);
        return Ok(ApiResponse.Ok("Parking slots for zone retrieved successfully", slots));
    }

    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.FacilityManager}")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ParkingSlotDto>>> Create([FromBody] CreateParkingSlotDto dto)
    {
        try
        {
            var slot = await _parkingSlotService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = slot.Id }, ApiResponse.Ok("Parking slot created successfully", slot));
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

    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.FacilityManager}")]
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<ParkingSlotDto>>> Update(string id, [FromBody] UpdateParkingSlotDto dto)
    {
        try
        {
            var slot = await _parkingSlotService.UpdateAsync(id, dto);
            if (slot == null)
            {
                return NotFound(ApiResponse.Fail("Parking slot not found."));
            }

            return Ok(ApiResponse.Ok("Parking slot updated successfully", slot));
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

    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.FacilityManager},{UserRoles.ParkingStaff}")]
    [HttpPatch("{id}/status")]
    public async Task<ActionResult<ApiResponse<ParkingSlotDto>>> UpdateStatus(string id, [FromBody] UpdateSlotStatusDto dto)
    {
        try
        {
            var slot = await _parkingSlotService.UpdateStatusAsync(id, dto.Status);
            if (slot == null)
            {
                return NotFound(ApiResponse.Fail("Parking slot not found."));
            }

            return Ok(ApiResponse.Ok("Parking slot status updated successfully", slot));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
    }

    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.FacilityManager}")]
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> Delete(string id)
    {
        var deleted = await _parkingSlotService.DeleteAsync(id);
        if (!deleted)
        {
            return NotFound(ApiResponse.Fail("Parking slot not found or already deleted."));
        }

        return Ok(ApiResponse.Ok("Parking slot soft deleted successfully"));
    }
}
