using Payment.Domain.Enums;

namespace Payment.Application.DTOs.FeePolicies;

public class FeePolicyDto
{
    public string Id { get; set; } = string.Empty;

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

    public int? OvertimeAfterHours { get; set; }

    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

public class CreateFeePolicyRequest
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

    public int? OvertimeAfterHours { get; set; }

    public DateTime? EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }
}

public class UpdateFeePolicyRequest
{
    public string Name { get; set; } = string.Empty;

    public PricingType PricingType { get; set; }

    public decimal BasePrice { get; set; }

    public decimal? HourlyPrice { get; set; }

    public decimal? DailyPrice { get; set; }

    public decimal? MonthlyPrice { get; set; }

    public decimal LostTicketFee { get; set; }

    public decimal OvertimeFee { get; set; }

    public int? OvertimeAfterHours { get; set; }

    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    public bool IsActive { get; set; }
}

public class CalculateFeeRequest
{
    public string BuildingId { get; set; } = string.Empty;

    public string VehicleTypeId { get; set; } = string.Empty;

    public DateTime CheckInTime { get; set; }

    public DateTime CheckOutTime { get; set; }

    public bool IsLostTicket { get; set; }

    /// <summary>When true, excludes normal base/hourly/daily/monthly charges and returns only overtime/lost-ticket penalties.</summary>
    public bool PenaltiesOnly { get; set; }
}

public class FeeBreakdownItem
{
    public string Description { get; set; } = string.Empty;

    public decimal Amount { get; set; }
}

public class CalculateFeeResponse
{
    public decimal Amount { get; set; }

    public string FeePolicyId { get; set; } = string.Empty;

    public PricingType PricingType { get; set; }

    public TimeSpan Duration { get; set; }

    public List<FeeBreakdownItem> Breakdown { get; set; } = new();
}
