using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

    public async Task<ActiveSubscriptionDto?> GetActiveByPlateAsync(string plateNumber, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.BaseUrl) || string.IsNullOrWhiteSpace(plateNumber))
            return null;

        var url = $"{_settings.BaseUrl.TrimEnd('/')}/api/v1/subscriptions/active/by-plate/{Uri.EscapeDataString(plateNumber)}";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);

            var authorization = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrWhiteSpace(authorization))
                request.Headers.TryAddWithoutValidation("Authorization", authorization);

            using var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                return null;

            var body = await response.Content.ReadAsStringAsync(ct);
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<ActiveSubscriptionDto>>(body, JsonOptions);
            return apiResponse?.Success == true ? apiResponse.Data : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Payment Service subscription active API for plate {Plate}", plateNumber);
            return null;
        }
    }

    private sealed class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
    }
}
