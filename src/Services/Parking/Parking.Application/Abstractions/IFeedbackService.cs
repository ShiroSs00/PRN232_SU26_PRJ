using Parking.Application.Common;
using Parking.Application.DTOs;
using Parking.Application.DTOs.Feedback;

namespace Parking.Application.Abstractions;

/// <summary>
/// Phản hồi của người dùng (mất thẻ, sai phí, khó tìm xe, vấn đề trong bãi...).
/// Driver tạo + xem phản hồi của mình; quản lý xem tất cả + trả lời.
/// </summary>
public interface IFeedbackService
{
    Task<Result<FeedbackDto>> CreateAsync(CreateFeedbackRequest request, string userId, CancellationToken ct = default);

    Task<Result<PagedResult<FeedbackDto>>> GetListAsync(FeedbackListQuery query, CancellationToken ct = default);

    Task<Result<FeedbackDto>> RespondAsync(string id, RespondFeedbackRequest request, string respondedByUserId, CancellationToken ct = default);
}
