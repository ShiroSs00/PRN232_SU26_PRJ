using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Report.Infrastructure.ReadModels;

// Lightweight read-only projections of documents owned by other services.
// Report has no project reference to Payment/Parking domains (microservice
// isolation), so these mirror only the fields needed for reporting.
// Field names/types match the source entities' default BSON serialization.

[BsonIgnoreExtraElements]
public class PaymentReadModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public int Status { get; set; }   // PaymentStatus: Pending=1, Paid=2, Failed=3, Refunded=4, Cancelled=5

    public int Method { get; set; }   // PaymentMethod: Cash=1, Card=2, EWallet=3, Mock=4

    public string? ShiftId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? PaidAt { get; set; }
}

[BsonIgnoreExtraElements]
public class SubscriptionReadModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    public decimal MonthlyFee { get; set; }

    public int Status { get; set; }   // SubscriptionStatus: Active=1, Expired=2, Suspended=3, Cancelled=4

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }
}

[BsonIgnoreExtraElements]
public class ParkingSlotReadModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    public int Status { get; set; }   // SlotStatus: Available=1, Occupied=2, Reserved=3, Maintenance=4, Locked=5
}

[BsonIgnoreExtraElements]
public class ParkingSessionReadModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    public int Status { get; set; }   // ParkingSessionStatus: Active=1, Completed=2, ...

    public DateTime CheckInTime { get; set; }

    public DateTime? CheckOutTime { get; set; }
}

[BsonIgnoreExtraElements]
public class ShiftReadModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    public string StaffUserId { get; set; } = string.Empty;

    public string BuildingId { get; set; } = string.Empty;

    public DateTime OpenedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    public decimal ExpectedCashAmount { get; set; }

    public decimal? CountedCashAmount { get; set; }

    public decimal DifferenceAmount { get; set; }

    public decimal TotalNonCashAmount { get; set; }

    public int TotalPayments { get; set; }
}
