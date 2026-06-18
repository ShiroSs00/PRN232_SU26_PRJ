using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payment.Application.Abstractions;
using Payment.Application.Common;
using Payment.Application.DTOs;
using Payment.Application.DTOs.FeePolicies;

namespace Payment.API.Controllers;

[ApiController]
[Route("api/v1/fee-policies")]
[Authorize(Roles = "Admin,FacilityManager")]
public class FeePoliciesController : ControllerBase
{
    private readonly IFeePolicyService _service;

    public FeePoliciesController(IFeePolicyService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<FeePolicyDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList(
        [FromQuery] string? buildingId,
        [FromQuery] string? vehicleTypeId,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _service.GetListAsync(buildingId, vehicleTypeId, isActive, page, pageSize, ct);
        return Ok(ApiResponse<PagedResult<FeePolicyDto>>.Ok(result.Value!));
    }

    [HttpGet("active")]
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff")]
    [ProducesResponseType(typeof(ApiResponse<List<FeePolicyDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActive(
        [FromQuery] string? buildingId,
        [FromQuery] string? vehicleTypeId,
        CancellationToken ct = default)
    {
        var result = await _service.GetActiveAsync(buildingId, vehicleTypeId, ct);
        return Ok(ApiResponse<List<FeePolicyDto>>.Ok(result.Value!));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<FeePolicyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        if (!result.Success)
            return NotFound(ApiResponse.Fail(result.Error!));
        return Ok(ApiResponse<FeePolicyDto>.Ok(result.Value!));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<FeePolicyDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateFeePolicyRequest request,
        CancellationToken ct)
    {
        var result = await _service.CreateAsync(request, ct);
        if (!result.Success)
            return BadRequest(ApiResponse.Fail(result.Error!));
        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value!.Id },
            ApiResponse<FeePolicyDto>.Ok(result.Value!, "Fee policy created."));
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<FeePolicyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        string id,
        [FromBody] UpdateFeePolicyRequest request,
        CancellationToken ct)
    {
        var result = await _service.UpdateAsync(id, request, ct);
        if (!result.Success)
        {
            var status = result.ErrorCode == PaymentErrorCodes.FeePolicyNotFound
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status400BadRequest;
            return StatusCode(status, ApiResponse.Fail(result.Error!));
        }
        return Ok(ApiResponse<FeePolicyDto>.Ok(result.Value!, "Fee policy updated."));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await _service.DeleteAsync(id, ct);
        if (!result.Success)
            return NotFound(ApiResponse.Fail(result.Error!));
        return Ok(ApiResponse.Ok("Fee policy deactivated."));
    }

    [HttpPost("calculate")]
    [Authorize(Roles = "Admin,FacilityManager,ParkingStaff")]
    [ProducesResponseType(typeof(ApiResponse<CalculateFeeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Calculate(
        [FromBody] CalculateFeeRequest request,
        CancellationToken ct)
    {
        var result = await _service.CalculateAsync(request, ct);
        if (!result.Success)
        {
            var status = result.ErrorCode switch
            {
                PaymentErrorCodes.ActivePolicyNotFound => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status400BadRequest
            };
            return StatusCode(status, ApiResponse.Fail(result.Error!));
        }
        return Ok(ApiResponse<CalculateFeeResponse>.Ok(result.Value!));
    }
}
