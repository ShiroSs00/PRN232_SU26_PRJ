namespace Parking.Domain.Enums;

public enum ParkingSessionLogAction
{
    CheckIn = 1,
    CheckOut = 2,
    ChangeSlot = 3,
    LostTicket = 4,
    ManualAdjustment = 5,
    Cancel = 6
}
