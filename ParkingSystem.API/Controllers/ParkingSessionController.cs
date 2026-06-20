using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkingSystem.Application.Common;
using ParkingSystem.Application.DTOs;
using ParkingSystem.Application.Services;
using ParkingSystem.Domain.Constants;

namespace ParkingSystem.API.Controllers;

[ApiController]
[Route("api/v1/parking-sessions")]
public class ParkingSessionController : ControllerBase
{
    private readonly IParkingSessionService _parkingSessionService;

    public ParkingSessionController(IParkingSessionService parkingSessionService)
    {
        _parkingSessionService = parkingSessionService;
    }

    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.FacilityManager},{UserRoles.ParkingStaff}")]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<ParkingSessionDto>>>> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? plateNumber)
    {
        var sessions = await _parkingSessionService.GetAllAsync(status, plateNumber);
        return Ok(ApiResponse.Ok("Parking sessions retrieved successfully", sessions));
    }

    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.FacilityManager},{UserRoles.ParkingStaff}")]
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ParkingSessionDto>>> GetById(string id)
    {
        var session = await _parkingSessionService.GetByIdAsync(id);
        if (session == null)
        {
            return NotFound(ApiResponse.Fail("Parking session not found."));
        }

        return Ok(ApiResponse.Ok("Parking session retrieved successfully", session));
    }

    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.FacilityManager},{UserRoles.ParkingStaff}")]
    [HttpGet("active/by-plate/{plateNumber}")]
    public async Task<ActionResult<ApiResponse<ParkingSessionDto>>> GetActiveByPlateNumber(string plateNumber)
    {
        var session = await _parkingSessionService.GetActiveByPlateNumberAsync(plateNumber);
        if (session == null)
        {
            return NotFound(ApiResponse.Fail("No active parking session found for this plate number."));
        }

        return Ok(ApiResponse.Ok("Active parking session retrieved successfully", session));
    }

    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.FacilityManager},{UserRoles.ParkingStaff}")]
    [HttpPost("check-in")]
    public async Task<ActionResult<ApiResponse<ParkingSessionDto>>> CheckIn([FromBody] CheckInDto dto)
    {
        try
        {
            var session = await _parkingSessionService.CheckInAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = session.Id }, ApiResponse.Ok("Vehicle checked in successfully", session));
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
    [HttpPost("{id}/check-out")]
    public async Task<ActionResult<ApiResponse<ParkingSessionDto>>> CheckOut(string id, [FromBody] CheckOutDto dto)
    {
        try
        {
            var session = await _parkingSessionService.CheckOutAsync(id, dto);
            if (session == null)
            {
                return NotFound(ApiResponse.Fail("Parking session not found."));
            }

            return Ok(ApiResponse.Ok("Vehicle checked out successfully. Payment pending.", session));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
    }

    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.FacilityManager},{UserRoles.ParkingStaff}")]
    [HttpPost("{id}/confirm-payment")]
    public async Task<ActionResult<ApiResponse<ParkingSessionDto>>> ConfirmPayment(string id, [FromBody] ConfirmPaymentDto dto)
    {
        try
        {
            var session = await _parkingSessionService.ConfirmPaymentAsync(id, dto);
            if (session == null)
            {
                return NotFound(ApiResponse.Fail("Parking session not found."));
            }

            return Ok(ApiResponse.Ok("Payment confirmed and parking session completed.", session));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
    }

    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.FacilityManager},{UserRoles.ParkingStaff}")]
    [HttpPost("{id}/mark-lost-ticket")]
    public async Task<ActionResult<ApiResponse<ParkingSessionDto>>> MarkLostTicket(string id)
    {
        try
        {
            var session = await _parkingSessionService.MarkLostTicketAsync(id);
            if (session == null)
            {
                return NotFound(ApiResponse.Fail("Parking session not found."));
            }

            return Ok(ApiResponse.Ok("Parking session marked as lost ticket.", session));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
    }

    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.FacilityManager},{UserRoles.ParkingStaff}")]
    [HttpPost("{id}/cancel")]
    public async Task<ActionResult<ApiResponse<ParkingSessionDto>>> Cancel(string id)
    {
        try
        {
            var session = await _parkingSessionService.CancelAsync(id);
            if (session == null)
            {
                return NotFound(ApiResponse.Fail("Parking session not found."));
            }

            return Ok(ApiResponse.Ok("Parking session cancelled and slot released.", session));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
    }
}
