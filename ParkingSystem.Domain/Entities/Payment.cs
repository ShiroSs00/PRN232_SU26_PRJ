using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ParkingSystem.Domain.Entities;

public class Payment
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("_id")]
    public string Id { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("parkingSessionId")]
    public string ParkingSessionId { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.Decimal128)]
    [BsonElement("amount")]
    public decimal Amount { get; set; }

    [BsonElement("method")]
    public string Method { get; set; } = string.Empty;

    [BsonElement("status")]
    public string Status { get; set; } = string.Empty;

    [BsonElement("paidAt")]
    public DateTime? PaidAt { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
}
