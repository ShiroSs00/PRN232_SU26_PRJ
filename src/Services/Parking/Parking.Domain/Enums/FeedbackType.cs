namespace Parking.Domain.Enums;

public enum FeedbackType
{
    LostTicket = 1,   // Mất thẻ xe
    WrongFee = 2,     // Sai phí
    HardToFind = 3,   // Khó tìm xe
    SlotOccupied = 4, // Slot bị chiếm
    Other = 5         // Vấn đề khác
}
