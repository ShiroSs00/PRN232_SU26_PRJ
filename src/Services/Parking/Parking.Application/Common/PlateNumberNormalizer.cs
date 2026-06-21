namespace Parking.Application.Common;

/// <summary>
/// Chuẩn hóa biển số dùng chung cho toàn Parking service: uppercase, bỏ khoảng trắng
/// và dấu gạch ngang. Đảm bảo "30A-12345", "30a 12345", "30A12345" quy về cùng một giá trị
/// để so khớp session/vehicle/reservation nhất quán.
/// </summary>
public static class PlateNumberNormalizer
{
    public static string Normalize(string? plateNumber)
    {
        if (string.IsNullOrWhiteSpace(plateNumber))
            return string.Empty;

        return plateNumber
            .Trim()
            .ToUpperInvariant()
            .Replace(" ", string.Empty)
            .Replace("-", string.Empty)
            .Replace(".", string.Empty);
    }
}
