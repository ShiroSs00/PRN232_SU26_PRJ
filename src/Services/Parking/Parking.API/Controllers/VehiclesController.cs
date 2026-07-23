using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parking.Application.Abstractions;
using Parking.Application.Common;
using Parking.Application.DTOs;
using Parking.Application.DTOs.Vehicles;

namespace Parking.API.Controllers;

[ApiController]
[Route("api/v1/vehicles")]
[Authorize]
public class VehiclesController : ControllerBase
{
    private readonly IVehicleService _service;

    public VehiclesController(IVehicleService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff,Driver")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<VehicleDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList(
        [FromQuery] string? search,
        [FromQuery] string? ownerUserId,
        [FromQuery] string? vehicleTypeId,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        // Driver chỉ được xem xe của chính mình: ép ownerUserId theo token.
        if (IsDriverOnly())
            ownerUserId = GetUserId();
        var result = await _service.GetListAsync(search, ownerUserId, vehicleTypeId, isActive, page, pageSize, ct);
        return Ok(ApiResponse<PagedResult<VehicleDto>>.Ok(result.Value!));
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff,Driver")]
    [ProducesResponseType(typeof(ApiResponse<VehicleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        if (!result.Success)
            return NotFound(ApiResponse.Fail(result.Error!));
        // Driver không được xem xe của người khác.
        if (IsDriverOnly() && result.Value!.OwnerUserId != GetUserId())
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail("Không có quyền."));
        return Ok(ApiResponse<VehicleDto>.Ok(result.Value!));
    }

    [HttpGet("by-plate/{plateNumber}")]
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff,Driver")]
    [ProducesResponseType(typeof(ApiResponse<VehicleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByPlate(string plateNumber, CancellationToken ct)
    {
        var result = await _service.GetByPlateAsync(plateNumber, ct);
        if (!result.Success)
        {
            var status = result.ErrorCode == ParkingErrorCodes.VehicleNotFound
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status400BadRequest;
            return StatusCode(status, ApiResponse.Fail(result.Error!));
        }
        if (IsDriverOnly() && result.Value!.OwnerUserId != GetUserId())
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail("Không có quyền."));
        return Ok(ApiResponse<VehicleDto>.Ok(result.Value!));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff,Driver")]
    [ProducesResponseType(typeof(ApiResponse<VehicleDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateVehicleRequest request,
        CancellationToken ct)
    {
        // Driver tự đăng ký xe: gắn chủ sở hữu là chính họ + tự điền thông tin từ JWT.
        if (IsDriverOnly())
        {
            request.OwnerUserId = GetUserId();
            if (string.IsNullOrWhiteSpace(request.OwnerName))
                request.OwnerName = User.FindFirstValue("full_name");
            if (string.IsNullOrWhiteSpace(request.OwnerPhone))
                request.OwnerPhone = User.FindFirstValue("phone_number");
            if (string.IsNullOrWhiteSpace(request.OwnerEmail))
                request.OwnerEmail = User.FindFirstValue(ClaimTypes.Email);
        }
        var result = await _service.CreateAsync(request, ct);
        if (!result.Success)
            return BadRequest(ApiResponse.Fail(result.Error!));
        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value!.Id },
            ApiResponse<VehicleDto>.Ok(result.Value!, "Vehicle created."));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff,Driver")]
    [ProducesResponseType(typeof(ApiResponse<VehicleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        string id,
        [FromBody] UpdateVehicleRequest request,
        CancellationToken ct)
    {
        // Driver chỉ sửa xe của mình.
        if (IsDriverOnly())
        {
            var existing = await _service.GetByIdAsync(id, ct);
            if (!existing.Success)
                return NotFound(ApiResponse.Fail(existing.Error!));
            if (existing.Value!.OwnerUserId != GetUserId())
                return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail("Không có quyền."));
            request.OwnerUserId = GetUserId();
        }
        var result = await _service.UpdateAsync(id, request, ct);
        if (!result.Success)
        {
            var status = result.ErrorCode == ParkingErrorCodes.VehicleNotFound
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status400BadRequest;
            return StatusCode(status, ApiResponse.Fail(result.Error!));
        }
        return Ok(ApiResponse<VehicleDto>.Ok(result.Value!, "Vehicle updated."));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await _service.DeleteAsync(id, ct);
        if (!result.Success)
            return NotFound(ApiResponse.Fail(result.Error!));
        return Ok(ApiResponse.Ok("Vehicle deactivated."));
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ??
        User.FindFirstValue("sub") ??
        string.Empty;

    // True nếu người dùng là Driver và KHÔNG kiêm vai trò quản trị/nhân viên.
    private bool IsDriverOnly() =>
        User.IsInRole("Driver")
        && !User.IsInRole("Admin")
        && !User.IsInRole("FacilityManager")
        && !User.IsInRole("ParkingStaff");
}
