using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Parking.Application.Abstractions;
using Parking.Application.Common;
using Parking.Application.Settings;

namespace Parking.Infrastructure.Services;

public sealed class PaymentClient : IPaymentClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly PaymentServiceSettings _settings;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<PaymentClient> _logger;

    public PaymentClient(
        HttpClient http,
        IOptions<PaymentServiceSettings> settings,
        IHttpContextAccessor httpContextAccessor,
        ILogger<PaymentClient> logger)
    {
        _http = http;
        _settings = settings.Value;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public Task<Result<ParkingPaymentDto>> CreateForParkingSessionAsync(
        CreateParkingPaymentCommand command,
        CancellationToken ct = default)
    {
        var payload = new
        {
            command.ParkingSessionId,
            SubscriptionId = (string?)null,
            command.PlateNumber,
            command.VehicleId,
            command.OwnerUserId,
            command.ShiftId,
            command.Amount,
            command.Method,
            Note = "Parking checkout"
        };

        return SendAsync(HttpMethod.Post, "/api/v1/payments", payload, ct);
    }

    public Task<Result<ParkingPaymentDto>> GetByIdAsync(
        string paymentId,
        CancellationToken ct = default) =>
        SendAsync(HttpMethod.Get, $"/api/v1/payments/{Uri.EscapeDataString(paymentId)}", null, ct);

    private async Task<Result<ParkingPaymentDto>> SendAsync(
        HttpMethod method,
        string path,
        object? payload,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_settings.BaseUrl))
            return Result<ParkingPaymentDto>.Fail(
                "Payment service base URL is not configured.",
                ParkingErrorCodes.PaymentServiceUnavailable);

        using var request = new HttpRequestMessage(
            method,
            $"{_settings.BaseUrl.TrimEnd('/')}{path}");
        if (payload is not null)
            request.Content = JsonContent.Create(payload, options: JsonOptions);

        var authorization = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(authorization))
            request.Headers.TryAddWithoutValidation("Authorization", authorization);

        try
        {
            using var response = await _http.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Payment Service request {Method} {Path} failed with {Status}: {Body}",
                    method, path, (int)response.StatusCode, body);
                var code = response.StatusCode == HttpStatusCode.NotFound
                    ? ParkingErrorCodes.PaymentNotFound
                    : ParkingErrorCodes.PaymentServiceUnavailable;
                return Result<ParkingPaymentDto>.Fail("Payment Service rejected the request.", code);
            }

            var apiResponse = JsonSerializer.Deserialize<ApiResponse<ParkingPaymentDto>>(body, JsonOptions);
            if (apiResponse?.Success != true || apiResponse.Data is null)
                return Result<ParkingPaymentDto>.Fail(
                    apiResponse?.Message ?? "Payment Service returned an invalid response.",
                    ParkingErrorCodes.PaymentServiceUnavailable);

            return Result<ParkingPaymentDto>.Ok(apiResponse.Data);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Payment Service {Method} {Path}", method, path);
            return Result<ParkingPaymentDto>.Fail(
                "Payment Service is unavailable.",
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
