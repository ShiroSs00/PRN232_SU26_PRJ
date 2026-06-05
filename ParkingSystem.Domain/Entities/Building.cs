using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ParkingSystem.Domain.Entities;

public class Building
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("_id")]
    public string Id { get; set; } = null!;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("address")]
    public string Address { get; set; } = string.Empty;

    [BsonElement("openingTime")]
    public string OpeningTime { get; set; } = string.Empty;

    [BsonElement("closingTime")]
    public string ClosingTime { get; set; } = string.Empty;

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
}
