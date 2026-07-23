using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Parking.Application.Abstractions;
using Parking.Application.Common;
using Parking.Application.Settings;

namespace Parking.Infrastructure.Services;

public class FeeCalculationClient : IFeeCalculationClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly PaymentServiceSettings _settings;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<FeeCalculationClient> _logger;

    public FeeCalculationClient(
        HttpClient http,
        IOptions<PaymentServiceSettings> settings,
        IHttpContextAccessor httpContextAccessor,
        ILogger<FeeCalculationClient> logger)
    {
        _http = http;
        _settings = settings.Value;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<Result<CalculatedParkingFee>> CalculateAsync(
        string buildingId,
        string vehicleTypeId,
        DateTime checkInTime,
        DateTime checkOutTime,
        bool isLostTicket,
        bool penaltiesOnly,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.BaseUrl))
            return Result<CalculatedParkingFee>.Fail("Payment service base URL is not configured.", "PAYMENT_SERVICE_NOT_CONFIGURED");

        var url = $"{_settings.BaseUrl.TrimEnd('/')}/api/v1/fee-policies/calculate";
        var payload = new
        {
            BuildingId = buildingId,
            VehicleTypeId = vehicleTypeId,
            CheckInTime = checkInTime,
            CheckOutTime = checkOutTime,
            IsLostTicket = isLostTicket,
            PenaltiesOnly = penaltiesOnly
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(payload, options: JsonOptions)
            };

            var authorization = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrWhiteSpace(authorization))
                request.Headers.TryAddWithoutValidation("Authorization", authorization);

            using var response = await _http.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Payment fee calculation failed with status {Status}: {Body}",
                    (int)response.StatusCode, body);
                return Result<CalculatedParkingFee>.Fail("Unable to calculate parking fee.", "FEE_CALCULATION_FAILED");
            }

            var apiResponse = JsonSerializer.Deserialize<ApiResponse<CalculatedParkingFee>>(body, JsonOptions);
            if (apiResponse?.Success != true || apiResponse.Data is null)
            {
                var message = string.IsNullOrWhiteSpace(apiResponse?.Message)
                    ? "Unable to calculate parking fee."
                    : apiResponse.Message;
                return Result<CalculatedParkingFee>.Fail(message, "FEE_CALCULATION_FAILED");
            }

            return Result<CalculatedParkingFee>.Ok(apiResponse.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling payment fee calculation API");
            return Result<CalculatedParkingFee>.Fail("Payment service is unavailable.", "PAYMENT_SERVICE_UNAVAILABLE");
        }
    }

    private sealed class ApiResponse<T>
    {
        public bool Success { get; set; }

        public string? Message { get; set; }

        public T? Data { get; set; }
    }
}
