using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parking.Application.Abstractions;
using Parking.Application.Common;
using Parking.Application.DTOs;
using Parking.Application.DTOs.Feedback;

namespace Parking.API.Controllers;

[ApiController]
[Route("api/v1/feedback")]
[Authorize]
public class FeedbackController : ControllerBase
{
    private readonly IFeedbackService _service;

    public FeedbackController(IFeedbackService service)
    {
        _service = service;
    }

    // Driver gửi phản hồi.
    [HttpPost]
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff,Driver")]
    [ProducesResponseType(typeof(ApiResponse<FeedbackDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateFeedbackRequest request, CancellationToken ct)
    {
        var result = await _service.CreateAsync(request, GetUserId(), ct);
        if (!result.Success)
            return BadRequest(ApiResponse.Fail(result.Error!));
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<FeedbackDto>.Ok(result.Value!, "Đã gửi phản hồi."));
    }

    // Phản hồi của chính người dùng.
    [HttpGet("my")]
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff,Driver")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<FeedbackDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMy(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _service.GetListAsync(new FeedbackListQuery
        {
            UserId = GetUserId(),
            Page = page,
            PageSize = pageSize
        }, ct);
        return Ok(ApiResponse<PagedResult<FeedbackDto>>.Ok(result.Value!));
    }

    // Quản lý xem tất cả phản hồi.
    [HttpGet]
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<FeedbackDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList([FromQuery] FeedbackListQuery query, CancellationToken ct)
    {
        var result = await _service.GetListAsync(query, ct);
        return Ok(ApiResponse<PagedResult<FeedbackDto>>.Ok(result.Value!));
    }

    // Quản lý trả lời phản hồi.
    [HttpPost("{id}/respond")]
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff")]
    [ProducesResponseType(typeof(ApiResponse<FeedbackDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Respond(string id, [FromBody] RespondFeedbackRequest request, CancellationToken ct)
    {
        var result = await _service.RespondAsync(id, request, GetUserId(), ct);
        if (!result.Success)
            return BadRequest(ApiResponse.Fail(result.Error!));
        return Ok(ApiResponse<FeedbackDto>.Ok(result.Value!, "Đã trả lời phản hồi."));
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ??
        User.FindFirstValue("sub") ??
        string.Empty;
}
