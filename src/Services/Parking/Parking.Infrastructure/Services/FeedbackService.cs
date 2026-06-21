using MongoDB.Bson;
using MongoDB.Driver;
using Parking.Application.Abstractions;
using Parking.Application.Common;
using Parking.Application.DTOs;
using Parking.Application.DTOs.Feedback;
using Parking.Domain.Entities;
using Parking.Domain.Enums;
using Parking.Infrastructure.Persistence;

namespace Parking.Infrastructure.Services;

public class FeedbackService : IFeedbackService
{
    private readonly MongoDbContext _db;

    public FeedbackService(MongoDbContext db)
    {
        _db = db;
    }

    public async Task<Result<FeedbackDto>> CreateAsync(
        CreateFeedbackRequest request, string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return Result<FeedbackDto>.Fail("Nội dung phản hồi không được để trống.", ParkingErrorCodes.ValidationFailed);

        var rating = request.Rating;
        if (rating < 1) rating = 1;
        if (rating > 5) rating = 5;

        var entity = new Feedback
        {
            UserId = userId,
            BuildingId = request.BuildingId,
            ParkingSessionId = request.ParkingSessionId,
            PlateNumber = request.PlateNumber?.Trim(),
            Rating = rating,
            Type = Enum.IsDefined(typeof(FeedbackType), request.Type)
                ? (FeedbackType)request.Type
                : FeedbackType.Other,
            Content = request.Content.Trim(),
            Status = FeedbackStatus.New,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        await _db.Feedbacks.InsertOneAsync(entity, cancellationToken: ct);
        return Result<FeedbackDto>.Ok(Map(entity));
    }

    public async Task<Result<PagedResult<FeedbackDto>>> GetListAsync(
        FeedbackListQuery query, CancellationToken ct = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : query.PageSize;
        if (pageSize > 200) pageSize = 200;

        var fb = Builders<Feedback>.Filter;
        var filters = new List<FilterDefinition<Feedback>>();

        if (!string.IsNullOrWhiteSpace(query.UserId))
            filters.Add(fb.Eq(x => x.UserId, query.UserId));
        if (!string.IsNullOrWhiteSpace(query.BuildingId))
            filters.Add(fb.Eq(x => x.BuildingId, query.BuildingId));
        if (query.Status.HasValue)
            filters.Add(fb.Eq(x => x.Status, (FeedbackStatus)query.Status.Value));
        if (query.Type.HasValue)
            filters.Add(fb.Eq(x => x.Type, (FeedbackType)query.Type.Value));

        var filter = filters.Count == 0 ? fb.Empty : fb.And(filters);
        var total = await _db.Feedbacks.CountDocumentsAsync(filter, cancellationToken: ct);

        var items = await _db.Feedbacks.Find(filter)
            .SortByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(ct);

        return Result<PagedResult<FeedbackDto>>.Ok(new PagedResult<FeedbackDto>
        {
            Items = items.Select(Map).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        });
    }

    public async Task<Result<FeedbackDto>> RespondAsync(
        string id, RespondFeedbackRequest request, string respondedByUserId, CancellationToken ct = default)
    {
        if (!ObjectId.TryParse(id, out _))
            return Result<FeedbackDto>.Fail("Phản hồi không tồn tại.", ParkingErrorCodes.ValidationFailed);

        var entity = await _db.Feedbacks.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (entity is null)
            return Result<FeedbackDto>.Fail("Phản hồi không tồn tại.", ParkingErrorCodes.ValidationFailed);

        var update = Builders<Feedback>.Update
            .Set(x => x.Response, request.Response?.Trim())
            .Set(x => x.Status, request.Status)
            .Set(x => x.RespondedByUserId, respondedByUserId)
            .Set(x => x.RespondedAt, DateTime.UtcNow)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        await _db.Feedbacks.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);

        entity = await _db.Feedbacks.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        return Result<FeedbackDto>.Ok(Map(entity!));
    }

    private static FeedbackDto Map(Feedback x) => new()
    {
        Id = x.Id,
        UserId = x.UserId,
        BuildingId = x.BuildingId,
        ParkingSessionId = x.ParkingSessionId,
        PlateNumber = x.PlateNumber,
        Rating = x.Rating,
        Type = x.Type,
        Content = x.Content,
        Status = x.Status,
        Response = x.Response,
        RespondedByUserId = x.RespondedByUserId,
        RespondedAt = x.RespondedAt,
        CreatedAt = x.CreatedAt,
    };
}
