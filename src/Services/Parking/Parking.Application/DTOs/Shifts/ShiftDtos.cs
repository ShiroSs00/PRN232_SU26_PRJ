namespace Parking.Application.DTOs.Shifts;

public sealed class ShiftDto
{
    public string Id { get; set; } = string.Empty;
    public string StaffUserId { get; set; } = string.Empty;
    public string BuildingId { get; set; } = string.Empty;
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public decimal ExpectedCashAmount { get; set; }
    public decimal TotalPayments { get; set; }
    public decimal TotalNonCashAmount { get; set; }
    public decimal? CountedCashAmount { get; set; }
    public decimal DifferenceAmount { get; set; }
    public int Status { get; set; }
    public string? Note { get; set; }
}

public sealed class ShiftListQuery
{
    public string? StaffUserId { get; set; }
    public string? BuildingId { get; set; }
    public int? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class OpenShiftRequest
{
    public string BuildingId { get; set; } = string.Empty;
}

public sealed class CloseShiftRequest
{
    public decimal CountedCashAmount { get; set; }
    public string? Note { get; set; }
}
