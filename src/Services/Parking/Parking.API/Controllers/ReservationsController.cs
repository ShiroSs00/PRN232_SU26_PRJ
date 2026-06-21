using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parking.Application.Abstractions;
using Parking.Application.Common;
using Parking.Application.DTOs;
using Parking.Application.DTOs.Reservations;

namespace Parking.API.Controllers;

[ApiController]
[Route("api/v1/reservations")]
[Authorize]
public class ReservationsController : ControllerBase
{
    private readonly IReservationService _service;

    public ReservationsController(IReservationService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff,Driver")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ReservationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList([FromQuery] ReservationListQuery query, CancellationToken ct)
    {
        // Driver chỉ xem đặt chỗ của chính mình: ép DriverUserId theo token.
        if (IsDriverOnly())
            query.DriverUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _service.GetListAsync(query, ct);
        return Ok(ApiResponse<PagedResult<ReservationDto>>.Ok(result.Value!));
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff")]
    [ProducesResponseType(typeof(ApiResponse<ReservationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        if (!result.Success)
            return NotFound(ApiResponse.Fail(result.Error!));
        return Ok(ApiResponse<ReservationDto>.Ok(result.Value!));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff,Driver")]
    [ProducesResponseType(typeof(ApiResponse<ReservationDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateReservationRequest request, CancellationToken ct)
    {
        var driverUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var result = await _service.CreateAsync(request, driverUserId, ct);
        if (!result.Success)
            return StatusCode(MapErrorStatus(result.ErrorCode), ApiResponse.Fail(result.Error!));
        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value!.Id },
            ApiResponse<ReservationDto>.Ok(result.Value!, "Reservation created."));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff")]
    [ProducesResponseType(typeof(ApiResponse<ReservationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateReservationRequest request, CancellationToken ct)
    {
        var result = await _service.UpdateAsync(id, request, ct);
        return MapMutationResult(result);
    }

    [HttpPost("{id}/confirm")]
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff")]
    [ProducesResponseType(typeof(ApiResponse<ReservationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Confirm(string id, CancellationToken ct)
    {
        var result = await _service.ConfirmAsync(id, ct);
        return MapMutationResult(result);
    }

    [HttpPost("{id}/cancel")]
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff,Driver")]
    [ProducesResponseType(typeof(ApiResponse<ReservationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(string id, [FromBody] CancelReservationRequest request, CancellationToken ct)
    {
        var cancelledByUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        // Drivers may only cancel their own reservation; staff/managers/admins are exempt.
        var enforceOwnership = User.IsInRole("Driver")
            && !User.IsInRole("Admin")
            && !User.IsInRole("FacilityManager")
            && !User.IsInRole("ParkingStaff");
        var result = await _service.CancelAsync(id, request, cancelledByUserId, enforceOwnership, ct);
        return MapMutationResult(result);
    }

    [HttpPost("{id}/check-in")]
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff")]
    [ProducesResponseType(typeof(ApiResponse<ReservationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CheckIn(string id, [FromBody] CheckInReservationRequest request, CancellationToken ct)
    {
        var result = await _service.CheckInAsync(id, request, ct);
        return MapMutationResult(result);
    }

    [HttpPost("{id}/expire")]
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff")]
    [ProducesResponseType(typeof(ApiResponse<ReservationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Expire(string id, CancellationToken ct)
    {
        var result = await _service.ExpireAsync(id, ct);
        return MapMutationResult(result);
    }

    private IActionResult MapMutationResult(Result<ReservationDto> result)
    {
        if (!result.Success)
            return StatusCode(MapErrorStatus(result.ErrorCode), ApiResponse.Fail(result.Error!));
        return Ok(ApiResponse<ReservationDto>.Ok(result.Value!));
    }

    private static int MapErrorStatus(string? errorCode) => errorCode switch
    {
        ParkingErrorCodes.ReservationNotFound => StatusCodes.Status404NotFound,
        ParkingErrorCodes.SlotNotFound => StatusCodes.Status404NotFound,
        ParkingErrorCodes.ReservationAccessDenied => StatusCodes.Status403Forbidden,
        ParkingErrorCodes.ReservationSlotConflict => StatusCodes.Status409Conflict,
        ParkingErrorCodes.SlotNotAvailable => StatusCodes.Status409Conflict,
        ParkingErrorCodes.InvalidReservationStatus => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status400BadRequest
    };

    // True nếu người dùng là Driver thuần (không kiêm vai trò quản trị/nhân viên).
    private bool IsDriverOnly() =>
        User.IsInRole("Driver")
        && !User.IsInRole("Admin")
        && !User.IsInRole("FacilityManager")
        && !User.IsInRole("ParkingStaff");
}
