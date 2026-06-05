using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ParkingSystem.Domain.Constants;

namespace ParkingSystem.Domain.Entities;

public class ParkingSession
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("_id")]
    public string Id { get; set; } = null!;

    [BsonElement("plateNumber")]
    public string PlateNumber { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("vehicleTypeId")]
    public string VehicleTypeId { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("parkingSlotId")]
    public string ParkingSlotId { get; set; } = string.Empty;

    [BsonElement("checkInTime")]
    public DateTime CheckInTime { get; set; } = DateTime.UtcNow;

    [BsonElement("checkOutTime")]
    public DateTime? CheckOutTime { get; set; }

    [BsonElement("entryGate")]
    public string? EntryGate { get; set; }

    [BsonElement("exitGate")]
    public string? ExitGate { get; set; }

    [BsonElement("status")]
    public string Status { get; set; } = ParkingSessionStatuses.Active;

    [BsonRepresentation(BsonType.Decimal128)]
    [BsonElement("totalFee")]
    public decimal? TotalFee { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("createdByUserId")]
    public string? CreatedByUserId { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("completedByUserId")]
    public string? CompletedByUserId { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
}
