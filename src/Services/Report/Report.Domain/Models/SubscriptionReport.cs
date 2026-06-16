namespace Report.Domain.Models;

public class SubscriptionReport
{
    public DateTime From { get; set; }

    public DateTime To { get; set; }

    public int ActiveSubscriptions { get; set; }

    public int ExpiringSubscriptions { get; set; }

    public int ExpiredSubscriptions { get; set; }

    public decimal MonthlyRecurringRevenue { get; set; }
}
