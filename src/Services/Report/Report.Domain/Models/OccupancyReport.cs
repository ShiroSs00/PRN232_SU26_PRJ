namespace Report.Domain.Models;

public class OccupancyReport
{
    public int TotalSlots { get; set; }

    public int AvailableSlots { get; set; }

    public int OccupiedSlots { get; set; }

    public int ReservedSlots { get; set; }

    public int MaintenanceSlots { get; set; }

    public decimal OccupancyRate { get; set; }
}
