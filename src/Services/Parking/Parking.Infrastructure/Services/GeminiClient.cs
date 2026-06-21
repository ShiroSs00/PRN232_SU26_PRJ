using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Parking.Application.Abstractions;
using Parking.Application.Common;
using Parking.Application.Settings;

namespace Parking.Infrastructure.Services;

/// <summary>
/// Gọi Google Gemini REST API (generateContent) để sinh văn bản.
/// Xác thực bằng API key qua query string ?key=. Lỗi cấu hình/mạng trả về Result.Fail
/// để tầng trên xử lý mềm (vẫn trả số liệu, chỉ thiếu phần phân tích AI).
/// </summary>
public class GeminiClient : IGeminiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _http;
    private readonly GeminiSettings _settings;
    private readonly ILogger<GeminiClient> _logger;

    public GeminiClient(HttpClient http, IOptions<GeminiSettings> settings, ILogger<GeminiClient> logger)
    {
        _http = http;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<Result<string>> GenerateAsync(string prompt, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            return Result<string>.Fail("Chưa cấu hình Gemini API key.", "GEMINI_NOT_CONFIGURED");

        var baseUrl = _settings.BaseUrl.TrimEnd('/');
        var url = $"{baseUrl}/models/{_settings.Model}:generateContent?key={_settings.ApiKey}";

        var payload = new
        {
            contents = new[]
            {
                new { role = "user", parts = new[] { new { text = prompt } } }
            }
        };

        try
        {
            // Gemini đôi khi trả 503 (quá tải tạm thời). Thử lại tối đa 3 lần với backoff ngắn.
            const int maxAttempts = 3;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                using var content = new StringContent(
                    JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                using var resp = await _http.PostAsync(url, content, ct);
                var body = await resp.Content.ReadAsStringAsync(ct);

                if (resp.IsSuccessStatusCode)
                {
                    var text = ExtractText(body);
                    if (string.IsNullOrWhiteSpace(text))
                        return Result<string>.Fail("Gemini không trả về nội dung.", "GEMINI_EMPTY");
                    return Result<string>.Ok(text);
                }

                bool transient = (int)resp.StatusCode is 503 or 429 or 500;
                _logger.LogWarning("Gemini API lỗi {Status} (lần {Attempt}/{Max}): {Body}",
                    (int)resp.StatusCode, attempt, maxAttempts, body);

                if (!transient || attempt == maxAttempts)
                    return Result<string>.Fail(
                        $"Gemini API lỗi ({(int)resp.StatusCode}).", "GEMINI_API_ERROR");

                await Task.Delay(TimeSpan.FromMilliseconds(700 * attempt), ct);
            }

            return Result<string>.Fail("Gemini API không phản hồi.", "GEMINI_API_ERROR");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi gọi Gemini API");
            return Result<string>.Fail("Không gọi được Gemini API.", "GEMINI_REQUEST_FAILED");
        }
    }

    /// <summary>Trích text từ candidates[0].content.parts[*].text.</summary>
    private static string ExtractText(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("candidates", out var candidates)
            || candidates.GetArrayLength() == 0)
            return string.Empty;

        var first = candidates[0];
        if (!first.TryGetProperty("content", out var contentEl)
            || !contentEl.TryGetProperty("parts", out var parts))
            return string.Empty;

        var sb = new StringBuilder();
        foreach (var part in parts.EnumerateArray())
        {
            if (part.TryGetProperty("text", out var textEl))
                sb.Append(textEl.GetString());
        }
        return sb.ToString();
    }
}
