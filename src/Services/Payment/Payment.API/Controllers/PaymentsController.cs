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
[Authorize]
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
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff")]
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
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff,Driver")]
    [ProducesResponseType(typeof(ApiResponse<PaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _payments.GetByIdAsync(id, ct);
        if (!result.Success)
            return NotFound(ApiResponse.Fail(result.Error!));
        if (!CanAccessPayment(result.Value!))
            return PaymentAccessDenied();
        return Ok(ApiResponse<PaymentDto>.Ok(result.Value!));
    }

    // Driver xem payment theo phiên (để thanh toán lượt gửi của mình).
    [HttpGet("by-session/{sessionId}")]
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff,Driver")]
    [ProducesResponseType(typeof(ApiResponse<List<PaymentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBySession(string sessionId, CancellationToken ct)
    {
        var result = await _payments.GetBySessionAsync(sessionId, ct);
        var items = result.Value!.Where(CanAccessPayment).ToList();
        return Ok(ApiResponse<List<PaymentDto>>.Ok(items));
    }

    // Driver xem payment theo vé tháng.
    [HttpGet("by-subscription/{subscriptionId}")]
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff,Driver")]
    [ProducesResponseType(typeof(ApiResponse<List<PaymentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBySubscription(string subscriptionId, CancellationToken ct)
    {
        var result = await _payments.GetBySubscriptionAsync(subscriptionId, ct);
        var items = result.Value!.Where(CanAccessPayment).ToList();
        return Ok(ApiResponse<List<PaymentDto>>.Ok(items));
    }

    [HttpGet("by-shift/{shiftId}/summary")]
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff")]
    [ProducesResponseType(typeof(ApiResponse<ShiftPaymentSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetShiftSummary(string shiftId, CancellationToken ct)
    {
        var result = await _payments.GetShiftSummaryAsync(shiftId, ct);
        return Ok(ApiResponse<ShiftPaymentSummaryDto>.Ok(result.Value!));
    }

    [HttpPost]
    // Driver creation is intentionally disabled: checkout payments are created by
    // trusted staff orchestration after the backend calculates the amount.
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff")]
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
                PaymentErrorCodes.InvalidShift => StatusCodes.Status409Conflict,
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
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff")]
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
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff")]
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

    // Driver tạo link PayOS để tự thanh toán.
    [HttpPost("{id}/payos-link")]
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff,Driver")]
    [ProducesResponseType(typeof(ApiResponse<PayOsLinkResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreatePayOsLink(string id, CancellationToken ct)
    {
        var paymentResult = await _payments.GetByIdAsync(id, ct);
        if (!paymentResult.Success)
            return NotFound(ApiResponse.Fail(paymentResult.Error!));
        if (!CanAccessPayment(paymentResult.Value!))
            return PaymentAccessDenied();

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

    // Driver check PayOS payment status (polling when webhook can't reach localhost).
    [HttpGet("{id}/payos-status")]
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff,Driver")]
    [ProducesResponseType(typeof(ApiResponse<PaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CheckPayOsStatus(string id, CancellationToken ct)
    {
        var paymentResult = await _payments.GetByIdAsync(id, ct);
        if (!paymentResult.Success)
            return NotFound(ApiResponse.Fail(paymentResult.Error!));
        if (!CanAccessPayment(paymentResult.Value!))
            return PaymentAccessDenied();

        var result = await _payos.CheckPaymentStatusAsync(id, ct);
        if (!result.Success)
        {
            var status = result.ErrorCode switch
            {
                PaymentErrorCodes.PaymentNotFound => StatusCodes.Status404NotFound,
                PaymentErrorCodes.PayOsSettingsMissing => StatusCodes.Status503ServiceUnavailable,
                PaymentErrorCodes.PayOsRequestFailed => StatusCodes.Status502BadGateway,
                _ => StatusCodes.Status400BadRequest
            };
            return StatusCode(status, ApiResponse.Fail(result.Error!));
        }
        return Ok(ApiResponse<PaymentDto>.Ok(result.Value!, "Payment status checked."));
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

    private bool CanAccessPayment(PaymentDto payment)
    {
        if (!IsDriverOnly())
            return true;

        var userId = GetUserId();
        return !string.IsNullOrWhiteSpace(userId) &&
               string.Equals(payment.OwnerUserId, userId, StringComparison.Ordinal);
    }

    private IActionResult PaymentAccessDenied() =>
        StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail("You do not have access to this payment."));

    private bool IsDriverOnly() =>
        User.IsInRole("Driver") &&
        !User.IsInRole("Admin") &&
        !User.IsInRole("FacilityManager") &&
        !User.IsInRole("ParkingStaff");

    private string? GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ??
        User.FindFirstValue("sub");
}
