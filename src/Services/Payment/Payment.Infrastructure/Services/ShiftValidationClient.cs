using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Payment.Application.Abstractions;
using Payment.Application.Common;
using Payment.Application.Settings;

namespace Payment.Infrastructure.Services;

public sealed class ShiftValidationClient : IShiftValidationClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly ParkingServiceSettings _settings;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ShiftValidationClient> _logger;

    public ShiftValidationClient(
        HttpClient http,
        IOptions<ParkingServiceSettings> settings,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ShiftValidationClient> logger)
    {
        _http = http;
        _settings = settings.Value;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<Result<CurrentShiftDto>> GetCurrentAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.BaseUrl))
            return Result<CurrentShiftDto>.Fail(
                "Parking service base URL is not configured.",
                PaymentErrorCodes.InvalidShift);

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{_settings.BaseUrl.TrimEnd('/')}/api/v1/shifts/current");
        var authorization = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(authorization))
            request.Headers.TryAddWithoutValidation("Authorization", authorization);

        try
        {
            using var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                return Result<CurrentShiftDto>.Fail(
                    "No matching open shift was found.",
                    PaymentErrorCodes.InvalidShift);

            var body = await response.Content.ReadAsStringAsync(ct);
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<CurrentShiftDto>>(body, JsonOptions);
            if (apiResponse?.Success != true || apiResponse.Data is null)
                return Result<CurrentShiftDto>.Fail(
                    apiResponse?.Message ?? "Parking Service returned an invalid response.",
                    PaymentErrorCodes.InvalidShift);

            return Result<CurrentShiftDto>.Ok(apiResponse.Data);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating the current parking shift.");
            return Result<CurrentShiftDto>.Fail(
                "Parking Service is unavailable.",
                PaymentErrorCodes.InvalidShift);
        }
    }

    private sealed class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
    }
}
