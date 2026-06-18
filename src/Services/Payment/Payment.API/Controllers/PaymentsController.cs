using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payment.Application.Abstractions;
using Payment.Application.Common;
using Payment.Application.DTOs;
using Payment.Application.DTOs.Payments;

namespace Payment.API.Controllers;

[ApiController]
[Route("api/v1/payments")]
[Authorize(Roles = "Admin,FacilityManager,ParkingStaff")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _payments;
    private readonly IPayOsService _payos;

    public PaymentsController(IPaymentService payments, IPayOsService payos)
    {
        _payments = payments;
        _payos = payos;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<PaymentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList(
        [FromQuery] string? sessionId,
        [FromQuery] string? plateNumber,
        [FromQuery] int? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _payments.GetListAsync(sessionId, plateNumber, status, page, pageSize, ct);
        return Ok(ApiResponse<PagedResult<PaymentDto>>.Ok(result.Value!));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<PaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _payments.GetByIdAsync(id, ct);
        if (!result.Success)
            return NotFound(ApiResponse.Fail(result.Error!));
        return Ok(ApiResponse<PaymentDto>.Ok(result.Value!));
    }

    [HttpGet("by-session/{sessionId}")]
    [ProducesResponseType(typeof(ApiResponse<List<PaymentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBySession(string sessionId, CancellationToken ct)
    {
        var result = await _payments.GetBySessionAsync(sessionId, ct);
        return Ok(ApiResponse<List<PaymentDto>>.Ok(result.Value!));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PaymentDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePaymentRequest request,
        CancellationToken ct)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var result = await _payments.CreateAsync(userId, request, ct);
        if (!result.Success)
        {
            var status = result.ErrorCode switch
            {
                PaymentErrorCodes.DuplicatePaymentForSession => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest
            };
            return StatusCode(status, ApiResponse.Fail(result.Error!));
        }
        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value!.Id },
            ApiResponse<PaymentDto>.Ok(result.Value!, "Payment created."));
    }

    [HttpPost("{id}/confirm")]
    [ProducesResponseType(typeof(ApiResponse<PaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Confirm(string id, CancellationToken ct)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var result = await _payments.ConfirmAsync(id, userId, ct);
        if (!result.Success)
            return MapError(result);
        return Ok(ApiResponse<PaymentDto>.Ok(result.Value!, "Payment confirmed."));
    }

    [HttpPost("{id}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<PaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(string id, CancellationToken ct)
    {
        var result = await _payments.CancelAsync(id, ct);
        if (!result.Success)
            return MapError(result);
        return Ok(ApiResponse<PaymentDto>.Ok(result.Value!, "Payment cancelled."));
    }

    [HttpPost("{id}/payos-link")]
    [ProducesResponseType(typeof(ApiResponse<PayOsLinkResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreatePayOsLink(string id, CancellationToken ct)
    {
        var result = await _payos.CreatePaymentLinkAsync(id, ct);
        if (!result.Success)
        {
            var status = result.ErrorCode switch
            {
                PaymentErrorCodes.PaymentNotFound => StatusCodes.Status404NotFound,
                PaymentErrorCodes.InvalidStatusTransition => StatusCodes.Status409Conflict,
                PaymentErrorCodes.PayOsSettingsMissing => StatusCodes.Status503ServiceUnavailable,
                PaymentErrorCodes.PayOsRequestFailed => StatusCodes.Status502BadGateway,
                _ => StatusCodes.Status400BadRequest
            };
            return StatusCode(status, ApiResponse.Fail(result.Error!));
        }
        return Ok(ApiResponse<PayOsLinkResponse>.Ok(result.Value!, "PayOS payment link created."));
    }

    /// <summary>PayOS webhook endpoint — anonymous; signature is verified inside the service.</summary>
    [HttpPost("webhook/payos")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> PayOsWebhook(CancellationToken ct)
    {
        // Always return 200 to prevent PayOS from retrying — verification result is logged server-side.
        using var reader = new StreamReader(Request.Body);
        var raw = await reader.ReadToEndAsync(ct);
        await _payos.HandleWebhookAsync(raw, ct);
        return Ok(new { success = true });
    }

    private IActionResult MapError(Result<PaymentDto> result)
    {
        var status = result.ErrorCode switch
        {
            PaymentErrorCodes.PaymentNotFound => StatusCodes.Status404NotFound,
            PaymentErrorCodes.InvalidStatusTransition => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };
        return StatusCode(status, ApiResponse.Fail(result.Error!));
    }

    private string? GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ??
        User.FindFirstValue("sub");
}
