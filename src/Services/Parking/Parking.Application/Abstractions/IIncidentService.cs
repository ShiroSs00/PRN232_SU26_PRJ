using Parking.Application.Common;
using Parking.Application.DTOs;
using Parking.Application.DTOs.Incidents;

namespace Parking.Application.Abstractions;

/// <summary>
/// Báo cáo sự cố trong bãi: mất vé, sai biển số, quá giờ, gửi sai khu vực,
/// xe chưa thanh toán... Nhân viên/quản lý tạo và theo dõi; quản lý xử lý (resolve/cancel).
/// </summary>
public interface IIncidentService
{
    Task<Result<PagedResult<IncidentReportDto>>> GetListAsync(IncidentListQuery query, CancellationToken ct = default);

    Task<Result<IncidentReportDto>> GetByIdAsync(string id, CancellationToken ct = default);

    Task<Result<IncidentReportDto>> CreateAsync(CreateIncidentRequest request, string reportedByUserId, CancellationToken ct = default);

    Task<Result<IncidentReportDto>> UpdateAsync(string id, UpdateIncidentRequest request, CancellationToken ct = default);

    Task<Result<IncidentReportDto>> ResolveAsync(string id, ResolveIncidentRequest request, string resolvedByUserId, CancellationToken ct = default);

    Task<Result<IncidentReportDto>> CancelAsync(string id, CancellationToken ct = default);
}
