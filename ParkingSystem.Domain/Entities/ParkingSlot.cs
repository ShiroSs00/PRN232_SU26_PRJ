using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ParkingSystem.Domain.Constants;

namespace ParkingSystem.Domain.Entities;

public class ParkingSlot
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
    [BsonElement("zoneId")]
    public string ZoneId { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("vehicleTypeId")]
    public string VehicleTypeId { get; set; } = string.Empty;

    [BsonElement("code")]
    public string Code { get; set; } = string.Empty;

    [BsonElement("status")]
    public string Status { get; set; } = ParkingSlotStatuses.Available;

    [BsonElement("row")]
    public int Row { get; set; }

    [BsonElement("column")]
    public int Column { get; set; }

    [BsonElement("rowSpan")]
    public int RowSpan { get; set; } = 1;

    [BsonElement("colSpan")]
    public int ColSpan { get; set; } = 1;

    [BsonElement("label")]
    public string? Label { get; set; }

    [BsonElement("positionX")]
    public double PositionX { get; set; }

    [BsonElement("positionY")]
    public double PositionY { get; set; }

    [BsonElement("width")]
    public double Width { get; set; }

    [BsonElement("height")]
    public double Height { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
}
