using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkingSystem.Application.Common;
using ParkingSystem.Application.DTOs;
using ParkingSystem.Application.Services;
using ParkingSystem.Domain.Constants;

namespace ParkingSystem.API.Controllers;

[ApiController]
[Route("api/v1/payments")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.FacilityManager},{UserRoles.ParkingStaff}")]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<PaymentDto>>>> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? method)
    {
        var payments = await _paymentService.GetAllAsync(status, method);
        return Ok(ApiResponse.Ok("Payments retrieved successfully", payments));
    }

    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.FacilityManager},{UserRoles.ParkingStaff}")]
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<PaymentDto>>> GetById(string id)
    {
        var payment = await _paymentService.GetByIdAsync(id);
        if (payment == null)
        {
            return NotFound(ApiResponse.Fail("Payment not found."));
        }

        return Ok(ApiResponse.Ok("Payment retrieved successfully", payment));
    }

    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.FacilityManager},{UserRoles.ParkingStaff}")]
    [HttpGet("by-session/{sessionId}")]
    public async Task<ActionResult<ApiResponse<PaymentDto>>> GetBySessionId(string sessionId)
    {
        var payment = await _paymentService.GetBySessionIdAsync(sessionId);
        if (payment == null)
        {
            return NotFound(ApiResponse.Fail("No payment found for this parking session."));
        }

        return Ok(ApiResponse.Ok("Payment for session retrieved successfully", payment));
    }

    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.FacilityManager},{UserRoles.ParkingStaff}")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<PaymentDto>>> Create([FromBody] CreatePaymentDto dto)
    {
        try
        {
            var payment = await _paymentService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = payment.Id }, ApiResponse.Ok("Payment created successfully", payment));
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
    [HttpPost("{id}/confirm")]
    public async Task<ActionResult<ApiResponse<PaymentDto>>> Confirm(string id, [FromBody] ConfirmPaymentDto dto)
    {
        try
        {
            var payment = await _paymentService.ConfirmPaymentAsync(id, dto);
            if (payment == null)
            {
                return NotFound(ApiResponse.Fail("Payment not found."));
            }

            return Ok(ApiResponse.Ok("Payment confirmed successfully.", payment));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
    }

    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.FacilityManager},{UserRoles.ParkingStaff}")]
    [HttpPost("{id}/refund")]
    public async Task<ActionResult<ApiResponse<PaymentDto>>> Refund(string id)
    {
        try
        {
            var payment = await _paymentService.RefundPaymentAsync(id);
            if (payment == null)
            {
                return NotFound(ApiResponse.Fail("Payment not found."));
            }

            return Ok(ApiResponse.Ok("Payment refunded successfully.", payment));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
    }
}
