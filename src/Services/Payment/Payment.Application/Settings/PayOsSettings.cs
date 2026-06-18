namespace Payment.Application.Settings;

public class PayOsSettings
{
    public string ClientId { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string ChecksumKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://api-merchant.payos.vn";

    public string ReturnUrl { get; set; } = string.Empty;

    public string CancelUrl { get; set; } = string.Empty;
}
