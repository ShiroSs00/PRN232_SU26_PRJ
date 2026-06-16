using Parking.Domain.Enums;
using Shared.Common.Entities;

namespace Parking.Domain.Entities;

public class Shift : BaseEntity
{
    public string StaffUserId { get; set; } = string.Empty;

    public string BuildingId { get; set; } = string.Empty;

    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ClosedAt { get; set; }

    public decimal ExpectedCashAmount { get; set; }

    public decimal TotalPayments { get; set; }

    public decimal TotalNonCashAmount { get; set; }

    public decimal? CountedCashAmount { get; set; }

    public decimal DifferenceAmount { get; set; }

    public ShiftStatus Status { get; set; } = ShiftStatus.Open;

    public string? Note { get; set; }
}
