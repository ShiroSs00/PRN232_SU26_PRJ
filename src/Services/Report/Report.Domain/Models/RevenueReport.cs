namespace Report.Domain.Models;

public class RevenueReport
{
    public DateTime From { get; set; }

    public DateTime To { get; set; }

    public decimal TotalRevenue { get; set; }

    public int TotalPayments { get; set; }
}
