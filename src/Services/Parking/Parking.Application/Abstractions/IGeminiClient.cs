using Parking.Application.Common;

namespace Parking.Application.Abstractions;

/// <summary>
/// Trừu tượng gọi mô hình sinh văn bản (Google Gemini) để tạo phân tích/giải thích.
/// Triển khai cụ thể nằm ở tầng Infrastructure.
/// </summary>
public interface IGeminiClient
{
    /// <summary>Gửi prompt và nhận về văn bản sinh ra. Lỗi cấu hình/mạng trả về Result.Fail.</summary>
    Task<Result<string>> GenerateAsync(string prompt, CancellationToken ct = default);
}
