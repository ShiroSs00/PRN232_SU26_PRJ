using MongoDB.Bson;
using MongoDB.Driver;
using Parking.Application.Abstractions;
using Parking.Application.Common;
using Parking.Application.DTOs;
using Parking.Application.DTOs.Incidents;
using Parking.Domain.Entities;
using Parking.Domain.Enums;
using Parking.Infrastructure.Persistence;

namespace Parking.Infrastructure.Services;

/// <summary>
/// Quản lý báo cáo sự cố trong bãi (mất vé, sai biển số, quá giờ, gửi sai khu vực,
/// xe chưa thanh toán...). Tạo -> Open; xử lý -> Resolved; hủy -> Cancelled.
/// </summary>
public class IncidentService : IIncidentService
{
    private readonly MongoDbContext _db;

    public IncidentService(MongoDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PagedResult<IncidentReportDto>>> GetListAsync(
        IncidentListQuery query, CancellationToken ct = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : query.PageSize;
        if (pageSize > 200) pageSize = 200;

        var fb = Builders<IncidentReport>.Filter;
        var filters = new List<FilterDefinition<IncidentReport>>();

        if (!string.IsNullOrWhiteSpace(query.BuildingId))
            filters.Add(fb.Eq(x => x.BuildingId, query.BuildingId));
        if (query.Status.HasValue)
            filters.Add(fb.Eq(x => x.Status, (IncidentStatus)query.Status.Value));
        if (query.Type.HasValue)
            filters.Add(fb.Eq(x => x.Type, (IncidentType)query.Type.Value));
        if (!string.IsNullOrWhiteSpace(query.PlateNumber))
            filters.Add(fb.Eq(x => x.PlateNumber, PlateNumberNormalizer.Normalize(query.PlateNumber)));
        if (!string.IsNullOrWhiteSpace(query.VehicleId))
            filters.Add(fb.Eq(x => x.VehicleId, query.VehicleId));
        if (!string.IsNullOrWhiteSpace(query.ParkingSessionId))
            filters.Add(fb.Eq(x => x.ParkingSessionId, query.ParkingSessionId));

        var filter = filters.Count == 0 ? fb.Empty : fb.And(filters);
        var total = await _db.IncidentReports.CountDocumentsAsync(filter, cancellationToken: ct);

        var items = await _db.IncidentReports.Find(filter)
            .SortByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(ct);

        return Result<PagedResult<IncidentReportDto>>.Ok(new PagedResult<IncidentReportDto>
        {
            Items = items.Select(Map).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        });
    }

    public async Task<Result<IncidentReportDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var entity = await FindAsync(id, ct);
        if (entity is null)
            return Result<IncidentReportDto>.Fail("Báo cáo sự cố không tồn tại.", ParkingErrorCodes.IncidentNotFound);
        return Result<IncidentReportDto>.Ok(Map(entity));
    }

    public async Task<Result<IncidentReportDto>> CreateAsync(
        CreateIncidentRequest request, string reportedByUserId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.BuildingId))
            return Result<IncidentReportDto>.Fail("Tòa nhà không được để trống.", ParkingErrorCodes.ValidationFailed);
        if (string.IsNullOrWhiteSpace(request.Title))
            return Result<IncidentReportDto>.Fail("Tiêu đề sự cố không được để trống.", ParkingErrorCodes.ValidationFailed);

        var plate = request.PlateNumber;
        var entity = new IncidentReport
        {
            BuildingId = request.BuildingId,
            ParkingSessionId = string.IsNullOrWhiteSpace(request.ParkingSessionId) ? null : request.ParkingSessionId,
            ParkingSlotId = string.IsNullOrWhiteSpace(request.ParkingSlotId) ? null : request.ParkingSlotId,
            VehicleId = string.IsNullOrWhiteSpace(request.VehicleId) ? null : request.VehicleId,
            PlateNumber = string.IsNullOrWhiteSpace(plate) ? null : PlateNumberNormalizer.Normalize(plate),
            OccupyingPlateNumber = string.IsNullOrWhiteSpace(request.OccupyingPlateNumber)
                ? null
                : PlateNumberNormalizer.Normalize(request.OccupyingPlateNumber),
            Title = request.Title.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            Type = Enum.IsDefined(typeof(IncidentType), request.Type)
                ? (IncidentType)request.Type
                : IncidentType.Other,
            Status = IncidentStatus.Open,
            ReportedByUserId = reportedByUserId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        await _db.IncidentReports.InsertOneAsync(entity, cancellationToken: ct);
        return Result<IncidentReportDto>.Ok(Map(entity));
    }

    public async Task<Result<IncidentReportDto>> UpdateAsync(
        string id, UpdateIncidentRequest request, CancellationToken ct = default)
    {
        var entity = await FindAsync(id, ct);
        if (entity is null)
            return Result<IncidentReportDto>.Fail("Báo cáo sự cố không tồn tại.", ParkingErrorCodes.IncidentNotFound);

        var update = Builders<IncidentReport>.Update.Set(x => x.UpdatedAt, DateTime.UtcNow);

        if (request.Title is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                return Result<IncidentReportDto>.Fail("Tiêu đề sự cố không được để trống.", ParkingErrorCodes.ValidationFailed);
            update = update.Set(x => x.Title, request.Title.Trim());
        }
        if (request.Description is not null)
            update = update.Set(x => x.Description, request.Description.Trim());
        if (request.Type.HasValue && Enum.IsDefined(typeof(IncidentType), request.Type.Value))
            update = update.Set(x => x.Type, (IncidentType)request.Type.Value);
        if (request.Status.HasValue && Enum.IsDefined(typeof(IncidentStatus), request.Status.Value))
            update = update.Set(x => x.Status, (IncidentStatus)request.Status.Value);
        if (request.ParkingSessionId is not null)
            update = update.Set(x => x.ParkingSessionId, string.IsNullOrWhiteSpace(request.ParkingSessionId) ? null : request.ParkingSessionId);
        if (request.ParkingSlotId is not null)
            update = update.Set(x => x.ParkingSlotId, string.IsNullOrWhiteSpace(request.ParkingSlotId) ? null : request.ParkingSlotId);
        if (request.VehicleId is not null)
            update = update.Set(x => x.VehicleId, string.IsNullOrWhiteSpace(request.VehicleId) ? null : request.VehicleId);
        if (request.PlateNumber is not null)
            update = update.Set(x => x.PlateNumber, string.IsNullOrWhiteSpace(request.PlateNumber) ? null : PlateNumberNormalizer.Normalize(request.PlateNumber));
        if (request.OccupyingPlateNumber is not null)
            update = update.Set(x => x.OccupyingPlateNumber, string.IsNullOrWhiteSpace(request.OccupyingPlateNumber) ? null : PlateNumberNormalizer.Normalize(request.OccupyingPlateNumber));

        await _db.IncidentReports.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);

        entity = await FindAsync(id, ct);
        return Result<IncidentReportDto>.Ok(Map(entity!));
    }

    public async Task<Result<IncidentReportDto>> ResolveAsync(
        string id, ResolveIncidentRequest request, string resolvedByUserId, CancellationToken ct = default)
    {
        var entity = await FindAsync(id, ct);
        if (entity is null)
            return Result<IncidentReportDto>.Fail("Báo cáo sự cố không tồn tại.", ParkingErrorCodes.IncidentNotFound);

        var update = Builders<IncidentReport>.Update
            .Set(x => x.Status, IncidentStatus.Resolved)
            .Set(x => x.ResolutionNote, request.ResolutionNote?.Trim())
            .Set(x => x.ResolvedByUserId, resolvedByUserId)
            .Set(x => x.ResolvedAt, DateTime.UtcNow)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        await _db.IncidentReports.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);

        entity = await FindAsync(id, ct);
        return Result<IncidentReportDto>.Ok(Map(entity!));
    }

    public async Task<Result<IncidentReportDto>> CancelAsync(string id, CancellationToken ct = default)
    {
        var entity = await FindAsync(id, ct);
        if (entity is null)
            return Result<IncidentReportDto>.Fail("Báo cáo sự cố không tồn tại.", ParkingErrorCodes.IncidentNotFound);

        var update = Builders<IncidentReport>.Update
            .Set(x => x.Status, IncidentStatus.Cancelled)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        await _db.IncidentReports.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);

        entity = await FindAsync(id, ct);
        return Result<IncidentReportDto>.Ok(Map(entity!));
    }

    private async Task<IncidentReport?> FindAsync(string id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id) || !ObjectId.TryParse(id, out _))
            return null;
        return await _db.IncidentReports.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
    }

    private static IncidentReportDto Map(IncidentReport x) => new()
    {
        Id = x.Id,
        BuildingId = x.BuildingId,
        ParkingSessionId = x.ParkingSessionId,
        ParkingSlotId = x.ParkingSlotId,
        VehicleId = x.VehicleId,
        PlateNumber = x.PlateNumber,
        OccupyingPlateNumber = x.OccupyingPlateNumber,
        Title = x.Title,
        Description = x.Description,
        Type = x.Type,
        Status = x.Status,
        ReportedByUserId = x.ReportedByUserId,
        ResolvedByUserId = x.ResolvedByUserId,
        ResolutionNote = x.ResolutionNote,
        ResolvedAt = x.ResolvedAt,
        CreatedAt = x.CreatedAt,
        UpdatedAt = x.UpdatedAt,
    };
}
