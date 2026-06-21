namespace Parking.Domain.Enums;

/// <summary>
/// Phân loại sự cố trong bãi: mất vé, sai biển số, quá giờ, gửi sai khu vực,
/// xe chưa thanh toán... Dùng để thống kê và xử lý theo nhóm.
/// </summary>
public enum IncidentType
{
    Other = 0,
    LostTicket = 1,
    WrongPlateNumber = 2,
    Overstay = 3,
    WrongZone = 4,
    UnpaidVehicle = 5,
    Damage = 6
}
