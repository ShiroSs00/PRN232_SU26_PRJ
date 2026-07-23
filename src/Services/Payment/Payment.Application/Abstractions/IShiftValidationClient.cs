using Payment.Application.Common;

namespace Payment.Application.Abstractions;

public sealed class CurrentShiftDto
{
    public string Id { get; set; } = string.Empty;
    public string StaffUserId { get; set; } = string.Empty;
    public string BuildingId { get; set; } = string.Empty;
    public int Status { get; set; }
}

public interface IShiftValidationClient
{
    Task<Result<CurrentShiftDto>> GetCurrentAsync(
        CancellationToken ct = default);
}
