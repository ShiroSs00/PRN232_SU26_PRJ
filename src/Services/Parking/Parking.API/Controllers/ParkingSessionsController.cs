using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parking.Application.Abstractions;
using Parking.Application.Common;
using Parking.Application.DTOs;
using Parking.Application.DTOs.Sessions;

namespace Parking.API.Controllers;

[ApiController]
[Route("api/v1/parking-sessions")]
[Authorize]
public class ParkingSessionsController : ControllerBase
{
    private readonly IParkingSessionService _service;

    public ParkingSessionsController(IParkingSessionService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ParkingSessionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList(
        [FromQuery] string? buildingId,
        [FromQuery] int? status,
        [FromQuery] string? plateNumber,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new SessionListQuery
        {
            BuildingId = buildingId,
            Status = status,
            PlateNumber = plateNumber,
            FromDate = fromDate,
            ToDate = toDate,
            Page = page,
            PageSize = pageSize
        };
        var result = await _service.GetListAsync(query, ct);
        return Ok(ApiResponse<PagedResult<ParkingSessionDto>>.Ok(result.Value!));
    }

    [HttpGet("my")]
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff,Driver")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ParkingSessionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMy(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _service.GetMySessionsAsync(GetUserId(), page, pageSize, ct);
        return Ok(ApiResponse<PagedResult<ParkingSessionDto>>.Ok(result.Value!));
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff")]
    [ProducesResponseType(typeof(ApiResponse<ParkingSessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        if (!result.Success)
            return NotFound(ApiResponse.Fail(result.Error!));
        return Ok(ApiResponse<ParkingSessionDto>.Ok(result.Value!));
    }

    [HttpGet("active/by-plate/{plateNumber}")]
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff")]
    [ProducesResponseType(typeof(ApiResponse<ParkingSessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetActiveByPlate(string plateNumber, CancellationToken ct)
    {
        var result = await _service.GetActiveByPlateAsync(plateNumber, ct);
        if (!result.Success)
        {
            var status = result.ErrorCode == ParkingErrorCodes.SessionNotFound
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status400BadRequest;
            return StatusCode(status, ApiResponse.Fail(result.Error!));
        }
        return Ok(ApiResponse<ParkingSessionDto>.Ok(result.Value!));
    }

    [HttpPost("check-in")]
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff")]
    [ProducesResponseType(typeof(ApiResponse<ParkingSessionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CheckIn(
        [FromBody] CheckInRequest request,
        CancellationToken ct)
    {
        var result = await _service.CheckInAsync(request, GetUserId(), ct);
        if (!result.Success)
            return MapError(result);
        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value!.Id },
            ApiResponse<ParkingSessionDto>.Ok(result.Value!, "Checked in."));
    }

    [HttpPost("{id}/check-out")]
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff")]
    [ProducesResponseType(typeof(ApiResponse<CheckoutResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PrepareCheckOut(
        string id,
        [FromBody] CheckOutRequest request,
        CancellationToken ct)
    {
        var result = await _service.PrepareCheckOutAsync(id, request, GetUserId(), ct);
        if (!result.Success)
            return MapCheckoutError(result);
        return Ok(ApiResponse<CheckoutResponse>.Ok(result.Value!, "Checkout prepared."));
    }

    [HttpPost("{id}/finalize-check-out")]
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff")]
    [ProducesResponseType(typeof(ApiResponse<CheckoutResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> FinalizeCheckOut(string id, CancellationToken ct)
    {
        var result = await _service.FinalizeCheckOutAsync(id, GetUserId(), ct);
        if (!result.Success)
            return MapCheckoutError(result);
        return Ok(ApiResponse<CheckoutResponse>.Ok(result.Value!, "Checkout finalized."));
    }

    [HttpPost("{id}/change-slot")]
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff")]
    [ProducesResponseType(typeof(ApiResponse<ParkingSessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeSlot(
        string id,
        [FromBody] ChangeSlotRequest request,
        CancellationToken ct)
    {
        var result = await _service.ChangeSlotAsync(id, request, GetUserId(), ct);
        if (!result.Success)
            return MapError(result);
        return Ok(ApiResponse<ParkingSessionDto>.Ok(result.Value!, "Slot changed."));
    }

    // Sửa thông tin xe (biển số/loại xe) của phiên đang gửi.
    [HttpPost("{id}/update-info")]
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff")]
    [ProducesResponseType(typeof(ApiResponse<ParkingSessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateInfo(
        string id,
        [FromBody] UpdateSessionInfoRequest request,
        CancellationToken ct)
    {
        var result = await _service.UpdateInfoAsync(id, request, GetUserId(), ct);
        if (!result.Success)
            return MapError(result);
        return Ok(ApiResponse<ParkingSessionDto>.Ok(result.Value!, "Đã cập nhật thông tin xe."));
    }

    // Đánh dấu phiên là ngoại lệ (quá hạn / bất thường).
    [HttpPost("{id}/mark-exception")]
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff")]
    [ProducesResponseType(typeof(ApiResponse<ParkingSessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkException(
        string id,
        [FromBody] MarkExceptionRequest request,
        CancellationToken ct)
    {
        var result = await _service.MarkExceptionAsync(id, request, GetUserId(), ct);
        if (!result.Success)
            return MapError(result);
        return Ok(ApiResponse<ParkingSessionDto>.Ok(result.Value!, "Đã đánh dấu ngoại lệ."));
    }

    // Phí tạm tính cho phiên đang gửi (Active) — Driver theo dõi lượt gửi của mình.
    [HttpGet("{id}/estimate-fee")]
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff,Driver")]
    [ProducesResponseType(typeof(ApiResponse<EstimateFeeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EstimateFee(string id, CancellationToken ct)
    {
        // Driver chỉ được xem phí tạm tính lượt gửi của xe mình; staff/manager xem mọi phiên.
        var enforceOwnership = User.IsInRole("Driver")
            && !User.IsInRole("Admin")
            && !User.IsInRole("FacilityManager")
            && !User.IsInRole("ParkingStaff");
        var result = await _service.EstimateFeeAsync(id, GetUserId(), enforceOwnership, ct);
        if (!result.Success)
        {
            var status = result.ErrorCode switch
            {
                ParkingErrorCodes.SessionNotFound => StatusCodes.Status404NotFound,
                ParkingErrorCodes.SessionNotActive => StatusCodes.Status409Conflict,
                ParkingErrorCodes.SessionAccessDenied => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status400BadRequest
            };
            return StatusCode(status, ApiResponse.Fail(result.Error!));
        }
        return Ok(ApiResponse<EstimateFeeResponse>.Ok(result.Value!));
    }

    private IActionResult MapCheckoutError(Result<CheckoutResponse> result)
    {
        var status = result.ErrorCode switch
        {
            ParkingErrorCodes.SessionNotFound => StatusCodes.Status404NotFound,
            ParkingErrorCodes.PaymentNotFound => StatusCodes.Status404NotFound,
            ParkingErrorCodes.SessionNotActive => StatusCodes.Status409Conflict,
            ParkingErrorCodes.PaymentNotPaid => StatusCodes.Status409Conflict,
            ParkingErrorCodes.PaymentMismatch => StatusCodes.Status409Conflict,
            ParkingErrorCodes.PaymentRequired => StatusCodes.Status409Conflict,
            ParkingErrorCodes.PaymentServiceUnavailable => StatusCodes.Status502BadGateway,
            _ => StatusCodes.Status400BadRequest
        };
        return StatusCode(status, ApiResponse.Fail(result.Error!));
    }

    private IActionResult MapError(Result<ParkingSessionDto> result)
    {
        var status = result.ErrorCode switch
        {
            ParkingErrorCodes.SessionNotFound => StatusCodes.Status404NotFound,
            ParkingErrorCodes.SlotNotFound => StatusCodes.Status404NotFound,
            ParkingErrorCodes.ActiveSessionExists => StatusCodes.Status409Conflict,
            ParkingErrorCodes.SlotNotAvailable => StatusCodes.Status409Conflict,
            ParkingErrorCodes.NoAvailableSlot => StatusCodes.Status409Conflict,
            ParkingErrorCodes.SessionNotActive => StatusCodes.Status409Conflict,
            ParkingErrorCodes.ZoneFull => StatusCodes.Status409Conflict,
            ParkingErrorCodes.InvalidSubscription => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status400BadRequest
        };
        return StatusCode(status, ApiResponse.Fail(result.Error!));
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ??
        User.FindFirstValue("sub") ??
        string.Empty;
}
