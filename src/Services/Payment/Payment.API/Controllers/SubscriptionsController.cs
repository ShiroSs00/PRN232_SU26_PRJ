using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Payment.Application.Abstractions;
using Payment.Application.Common;
using Payment.Application.DTOs;
using Payment.Application.DTOs.Subscriptions;

namespace Payment.API.Controllers;

[ApiController]
[Route("api/v1/subscriptions")]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _service;

    public SubscriptionsController(ISubscriptionService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<SubscriptionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList(
        [FromQuery] int? status,
        [FromQuery] string? buildingId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _service.GetListAsync(status, buildingId, page, pageSize, ct);
        return Ok(ApiResponse<PagedResult<SubscriptionDto>>.Ok(result.Value!));
    }

    [HttpGet("my")]
    [Authorize(Roles = "Driver")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<SubscriptionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMySubscriptions(
        [FromQuery] int? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _service.GetMySubscriptionsAsync(GetUserId(), status, page, pageSize, ct);
        return Ok(ApiResponse<PagedResult<SubscriptionDto>>.Ok(result.Value!));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<SubscriptionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        if (!result.Success)
            return NotFound(ApiResponse.Fail(result.Error!));
        return Ok(ApiResponse<SubscriptionDto>.Ok(result.Value!));
    }

    [HttpGet("active/by-plate/{plateNumber}")]
    [ProducesResponseType(typeof(ApiResponse<SubscriptionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetActiveByPlate(string plateNumber, CancellationToken ct)
    {
        var result = await _service.GetActiveByPlateAsync(plateNumber, ct);
        if (result.Value is null)
            return NotFound(ApiResponse.Fail("No active subscription for this plate."));
        return Ok(ApiResponse<SubscriptionDto>.Ok(result.Value!));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,FacilityManager")]
    [ProducesResponseType(typeof(ApiResponse<SubscriptionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateSubscriptionRequest request,
        CancellationToken ct)
    {
        var result = await _service.CreateAsync(request, ct);
        if (!result.Success)
            return BadRequest(ApiResponse.Fail(result.Error!));
        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value!.Id },
            ApiResponse<SubscriptionDto>.Ok(result.Value!, "Subscription created."));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,FacilityManager")]
    [ProducesResponseType(typeof(ApiResponse<SubscriptionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        string id,
        [FromBody] UpdateSubscriptionRequest request,
        CancellationToken ct)
    {
        var result = await _service.UpdateAsync(id, request, ct);
        if (!result.Success)
            return MapError(result);
        return Ok(ApiResponse<SubscriptionDto>.Ok(result.Value!, "Subscription updated."));
    }

    [HttpPost("request")]
    [Authorize(Roles = "Driver")]
    [ProducesResponseType(typeof(ApiResponse<SubscriptionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Request(
        [FromBody] CreateSubscriptionRequest request,
        CancellationToken ct)
    {
        var result = await _service.RequestAsync(request, GetUserId(), ct);
        if (!result.Success)
            return BadRequest(ApiResponse.Fail(result.Error!));
        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value!.Id },
            ApiResponse<SubscriptionDto>.Ok(result.Value!, "Subscription request created."));
    }

    [HttpPost("{id}/approve")]
    [Authorize(Roles = "Admin,FacilityManager")]
    [ProducesResponseType(typeof(ApiResponse<SubscriptionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Approve(string id, CancellationToken ct)
    {
        var result = await _service.ApproveAsync(id, GetUserId(), ct);
        if (!result.Success)
            return MapError(result);
        return Ok(ApiResponse<SubscriptionDto>.Ok(result.Value!, "Subscription approved."));
    }

    [HttpPost("{id}/reject")]
    [Authorize(Roles = "Admin,FacilityManager")]
    [ProducesResponseType(typeof(ApiResponse<SubscriptionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Reject(
        string id,
        [FromBody] RejectSubscriptionRequest? request,
        CancellationToken ct)
    {
        var result = await _service.RejectAsync(id, request?.Reason, ct);
        if (!result.Success)
            return MapError(result);
        return Ok(ApiResponse<SubscriptionDto>.Ok(result.Value!, "Subscription rejected."));
    }

    [HttpPost("{id}/renew")]
    [Authorize(Roles = "Admin,FacilityManager")]
    [ProducesResponseType(typeof(ApiResponse<SubscriptionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Renew(
        string id,
        [FromBody] RenewSubscriptionRequest request,
        CancellationToken ct)
    {
        var result = await _service.RenewAsync(id, request, ct);
        if (!result.Success)
            return MapError(result);
        return Ok(ApiResponse<SubscriptionDto>.Ok(result.Value!, "Subscription renewed."));
    }

    [HttpPost("{id}/suspend")]
    [Authorize(Roles = "Admin,FacilityManager")]
    [ProducesResponseType(typeof(ApiResponse<SubscriptionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Suspend(string id, CancellationToken ct)
    {
        var result = await _service.SuspendAsync(id, ct);
        if (!result.Success)
            return MapError(result);
        return Ok(ApiResponse<SubscriptionDto>.Ok(result.Value!, "Subscription suspended."));
    }

    [HttpPost("{id}/cancel")]
    [Authorize(Roles = "Admin,FacilityManager")]
    [ProducesResponseType(typeof(ApiResponse<SubscriptionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(string id, CancellationToken ct)
    {
        var result = await _service.CancelAsync(id, ct);
        if (!result.Success)
            return MapError(result);
        return Ok(ApiResponse<SubscriptionDto>.Ok(result.Value!, "Subscription cancelled."));
    }

    private IActionResult MapError(Result<SubscriptionDto> result)
    {
        var status = result.ErrorCode switch
        {
            PaymentErrorCodes.SubscriptionNotFound => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status400BadRequest
        };
        return StatusCode(status, ApiResponse.Fail(result.Error!));
    }

    private string? GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ??
        User.FindFirstValue("sub");
}
