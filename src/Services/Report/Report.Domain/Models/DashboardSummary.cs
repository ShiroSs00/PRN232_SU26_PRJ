namespace Report.Domain.Models;

public class DashboardSummary
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    public int TotalSlots { get; set; }

    public int OccupiedSlots { get; set; }

    public int AvailableSlots { get; set; }

    public int ActiveSessions { get; set; }

    public int ActiveSubscriptions { get; set; }

    public decimal TodayRevenue { get; set; }

    public decimal OccupancyRate { get; set; }
}
