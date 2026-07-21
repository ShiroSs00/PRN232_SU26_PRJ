using Parking.Domain.Enums;

namespace Parking.Application.DTOs.Feedback;

public class FeedbackDto
{
    public string Id { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? BuildingId { get; set; }
    public string? ParkingSessionId { get; set; }
    public string? PlateNumber { get; set; }
    public FeedbackType Type { get; set; }
    public string Content { get; set; } = string.Empty;
    public FeedbackStatus Status { get; set; }
    public string? Response { get; set; }
    public string? RespondedByUserId { get; set; }
    public DateTime? RespondedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateFeedbackRequest
{
    public int Type { get; set; } = (int)FeedbackType.Other;
    public string Content { get; set; } = string.Empty;
    public string? BuildingId { get; set; }
    public string? ParkingSessionId { get; set; }
    public string? PlateNumber { get; set; }
}

public class RespondFeedbackRequest
{
    public string Response { get; set; } = string.Empty;
    public FeedbackStatus Status { get; set; } = FeedbackStatus.Resolved;
}

public class FeedbackListQuery
{
    public string? UserId { get; set; }
    public string? BuildingId { get; set; }
    public int? Status { get; set; }
    public int? Type { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
