namespace Report.Domain.Models;

public class VehicleFlowReport
{
    public DateTime From { get; set; }

    public DateTime To { get; set; }

    public int CheckIns { get; set; }

    public int CheckOuts { get; set; }
}
