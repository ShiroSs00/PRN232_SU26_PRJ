using Parking.Domain.Enums;
using Shared.Common.Entities;

namespace Parking.Domain.Entities;

public class Feedback : AuditableEntity
{
    public string? UserId { get; set; }

    public string? BuildingId { get; set; }

    public string? ParkingSessionId { get; set; }

    public string? PaymentId { get; set; }

    public string? VehicleId { get; set; }

    public string? PlateNumber { get; set; }

    public int Rating { get; set; }

    public FeedbackType Type { get; set; } = FeedbackType.Other;

    public string Content { get; set; } = string.Empty;

    public FeedbackStatus Status { get; set; } = FeedbackStatus.New;

    public string? Response { get; set; }

    public string? RespondedByUserId { get; set; }

    public DateTime? RespondedAt { get; set; }
}
