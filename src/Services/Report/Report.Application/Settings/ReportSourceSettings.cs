namespace Report.Application.Settings;

/// <summary>
/// Names of the source databases the Report service reads from (read-only),
/// all on the same MongoDB Atlas cluster as the report database.
/// </summary>
public class ReportSourceSettings
{
    public string PaymentDatabaseName { get; set; } = "parking_payment_db";

    public string ParkingDatabaseName { get; set; } = "parking_main_db";
}
