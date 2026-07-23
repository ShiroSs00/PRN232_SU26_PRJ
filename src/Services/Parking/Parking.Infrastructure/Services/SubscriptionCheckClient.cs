using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Parking.Application.Common;
using Parking.Application.Abstractions;
using Parking.Application.Settings;

namespace Parking.Infrastructure.Services;

public class SubscriptionCheckClient : ISubscriptionCheckClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly PaymentServiceSettings _settings;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<SubscriptionCheckClient> _logger;

    public SubscriptionCheckClient(
        HttpClient http,
        IOptions<PaymentServiceSettings> settings,
        IHttpContextAccessor httpContextAccessor,
        ILogger<SubscriptionCheckClient> logger)
    {
        _http = http;
        _settings = settings.Value;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<Result<ActiveSubscriptionDto?>> GetActiveAsync(
        string plateNumber,
        string buildingId,
        string vehicleTypeId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.BaseUrl))
            return Result<ActiveSubscriptionDto?>.Fail(
                "Payment service base URL is not configured.",
                ParkingErrorCodes.PaymentServiceUnavailable);
        if (string.IsNullOrWhiteSpace(plateNumber) ||
            string.IsNullOrWhiteSpace(buildingId) ||
            string.IsNullOrWhiteSpace(vehicleTypeId))
            return Result<ActiveSubscriptionDto?>.Fail(
                "Plate number, building and vehicle type are required for subscription lookup.",
                ParkingErrorCodes.ValidationFailed);

        var url = $"{_settings.BaseUrl.TrimEnd('/')}/api/v1/subscriptions/active/by-plate/{Uri.EscapeDataString(plateNumber)}" +
                  $"?buildingId={Uri.EscapeDataString(buildingId)}&vehicleTypeId={Uri.EscapeDataString(vehicleTypeId)}";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            var authorization = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrWhiteSpace(authorization))
                request.Headers.TryAddWithoutValidation("Authorization", authorization);

            using var response = await _http.SendAsync(request, ct);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return Result<ActiveSubscriptionDto?>.Ok(null);

            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Payment Service subscription lookup failed with status {Status}: {Body}",
                    (int)response.StatusCode,
                    body);
                return Result<ActiveSubscriptionDto?>.Fail(
                    "Payment service rejected subscription lookup.",
                    ParkingErrorCodes.PaymentServiceUnavailable);
            }

            var apiResponse = JsonSerializer.Deserialize<ApiResponse<ActiveSubscriptionDto>>(body, JsonOptions);
            if (apiResponse?.Success != true || apiResponse.Data is null)
                return Result<ActiveSubscriptionDto?>.Fail(
                    "Payment service returned an invalid subscription response.",
                    ParkingErrorCodes.PaymentServiceUnavailable);

            return Result<ActiveSubscriptionDto?>.Ok(apiResponse.Data);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Payment Service subscription API for plate {Plate}", plateNumber);
            return Result<ActiveSubscriptionDto?>.Fail(
                "Payment service is unavailable.",
                ParkingErrorCodes.PaymentServiceUnavailable);
        }
    }
    private sealed class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
    }
}
