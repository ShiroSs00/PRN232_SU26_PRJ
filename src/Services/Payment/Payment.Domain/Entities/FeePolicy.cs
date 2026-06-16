using Payment.Domain.Enums;
using Shared.Common.Entities;

namespace Payment.Domain.Entities;

public class FeePolicy : AuditableEntity
{
    public string BuildingId { get; set; } = string.Empty;

    public string VehicleTypeId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public PricingType PricingType { get; set; }

    public decimal BasePrice { get; set; }

    public decimal? HourlyPrice { get; set; }

    public decimal? DailyPrice { get; set; }

    public decimal? MonthlyPrice { get; set; }

    public decimal LostTicketFee { get; set; }

    public decimal OvertimeFee { get; set; }

    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;

    public DateTime? EffectiveTo { get; set; }
}
