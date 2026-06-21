namespace Parking.Application.Settings;

/// <summary>
/// Cấu hình gọi Google Gemini API. Nạp từ section "GeminiSettings" (appsettings.Local.json).
/// </summary>
public class GeminiSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-3.1-flash-lite";
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";
}
