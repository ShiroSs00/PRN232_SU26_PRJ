using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ParkingSystem.Domain.Entities;

public class FeePolicy
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("_id")]
    public string Id { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("vehicleTypeId")]
    public string VehicleTypeId { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("pricingType")]
    public string PricingType { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.Decimal128)]
    [BsonElement("basePrice")]
    public decimal BasePrice { get; set; }

    [BsonRepresentation(BsonType.Decimal128)]
    [BsonElement("hourlyPrice")]
    public decimal? HourlyPrice { get; set; }

    [BsonRepresentation(BsonType.Decimal128)]
    [BsonElement("dailyPrice")]
    public decimal? DailyPrice { get; set; }

    [BsonRepresentation(BsonType.Decimal128)]
    [BsonElement("lostTicketFee")]
    public decimal? LostTicketFee { get; set; }

    [BsonRepresentation(BsonType.Decimal128)]
    [BsonElement("overtimeFee")]
    public decimal? OvertimeFee { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("effectiveFrom")]
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;

    [BsonElement("effectiveTo")]
    public DateTime? EffectiveTo { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
}
