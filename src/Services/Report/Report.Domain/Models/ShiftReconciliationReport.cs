namespace Report.Domain.Models;

public class ShiftReconciliationReport
{
    public string ShiftId { get; set; } = string.Empty;

    public string StaffUserId { get; set; } = string.Empty;

    public string BuildingId { get; set; } = string.Empty;

    public DateTime OpenedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    public decimal ExpectedCashAmount { get; set; }

    public decimal? CountedCashAmount { get; set; }

    public decimal DifferenceAmount { get; set; }

    public decimal TotalNonCashAmount { get; set; }

    public int TotalPayments { get; set; }
}
