using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ParkingSystem.Domain.Entities;

public class Zone
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("_id")]
    public string Id { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("buildingId")]
    public string BuildingId { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("floorId")]
    public string FloorId { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("vehicleTypeId")]
    public string VehicleTypeId { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("capacity")]
    public int Capacity { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
}
