using MongoDB.Driver;
using Report.Application.Abstractions;
using Report.Application.Common;
using Report.Domain.Models;
using Report.Infrastructure.Persistence;
using Report.Infrastructure.ReadModels;

namespace Report.Infrastructure.Services;

public class ReportService : IReportService
{
    // Mirror of source-service enum values (read as ints from BSON).
    private const int PaymentPaid = 2;
    private const int SubscriptionActive = 1;
    private const int SubscriptionExpired = 2;
    private const int SlotAvailable = 1;
    private const int SlotOccupied = 2;
    private const int SlotReserved = 3;
    private const int SlotMaintenance = 4;
    private const int SessionActive = 1;

    // Lệch múi giờ địa phương (GMT+7) để quy đổi giờ UTC khi tính khung giờ cao điểm.
    private const int LocalUtcOffsetHours = 7;

    private static int ToLocalHour(DateTime utc) => (utc.Hour + LocalUtcOffsetHours) % 24;

    private readonly MongoDbContext _db;

    public ReportService(MongoDbContext db)
    {
        _db = db;
    }

    public async Task<Result<DashboardSummary>> GetDashboardAsync(CancellationToken ct = default)
    {
        var todayStart = DateTime.UtcNow.Date;
        var todayEnd = todayStart.AddDays(1);

        var totalSlots = (int)await _db.ParkingSlots.CountDocumentsAsync(FilterDefinition<ParkingSlotReadModel>.Empty, cancellationToken: ct);
        var occupiedSlots = (int)await _db.ParkingSlots.CountDocumentsAsync(x => x.Status == SlotOccupied, cancellationToken: ct);
        var availableSlots = (int)await _db.ParkingSlots.CountDocumentsAsync(x => x.Status == SlotAvailable, cancellationToken: ct);
        var activeSessions = (int)await _db.ParkingSessions.CountDocumentsAsync(x => x.Status == SessionActive, cancellationToken: ct);
        var activeSubs = (int)await _db.Subscriptions.CountDocumentsAsync(x => x.Status == SubscriptionActive, cancellationToken: ct);

        var todayRevenue = await SumPaidAsync(todayStart, todayEnd, ct);

        var summary = new DashboardSummary
        {
            GeneratedAt = DateTime.UtcNow,
            TotalSlots = totalSlots,
            OccupiedSlots = occupiedSlots,
            AvailableSlots = availableSlots,
            ActiveSessions = activeSessions,
            ActiveSubscriptions = activeSubs,
            TodayRevenue = todayRevenue,
            OccupancyRate = totalSlots > 0 ? Math.Round((decimal)occupiedSlots / totalSlots * 100, 2) : 0
        };
        return Result<DashboardSummary>.Ok(summary);
    }

    public async Task<Result<RevenueReport>> GetRevenueAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        if (to < from)
            return Result<RevenueReport>.Fail("'to' must be after 'from'.", ReportErrorCodes.ValidationFailed);

        var filter = Builders<PaymentReadModel>.Filter.And(
            Builders<PaymentReadModel>.Filter.Eq(x => x.Status, PaymentPaid),
            Builders<PaymentReadModel>.Filter.Gte(x => x.PaidAt, from),
            Builders<PaymentReadModel>.Filter.Lt(x => x.PaidAt, to));

        var paid = await _db.Payments.Find(filter).ToListAsync(ct);

        return Result<RevenueReport>.Ok(new RevenueReport
        {
            From = from,
            To = to,
            TotalRevenue = paid.Sum(x => x.Amount),
            TotalPayments = paid.Count
        });
    }

    public async Task<Result<OccupancyReport>> GetOccupancyAsync(CancellationToken ct = default)
    {
        var total = (int)await _db.ParkingSlots.CountDocumentsAsync(FilterDefinition<ParkingSlotReadModel>.Empty, cancellationToken: ct);
        var available = (int)await _db.ParkingSlots.CountDocumentsAsync(x => x.Status == SlotAvailable, cancellationToken: ct);
        var occupied = (int)await _db.ParkingSlots.CountDocumentsAsync(x => x.Status == SlotOccupied, cancellationToken: ct);
        var reserved = (int)await _db.ParkingSlots.CountDocumentsAsync(x => x.Status == SlotReserved, cancellationToken: ct);
        var maintenance = (int)await _db.ParkingSlots.CountDocumentsAsync(x => x.Status == SlotMaintenance, cancellationToken: ct);

        return Result<OccupancyReport>.Ok(new OccupancyReport
        {
            TotalSlots = total,
            AvailableSlots = available,
            OccupiedSlots = occupied,
            ReservedSlots = reserved,
            MaintenanceSlots = maintenance,
            OccupancyRate = total > 0 ? Math.Round((decimal)occupied / total * 100, 2) : 0
        });
    }

    public async Task<Result<VehicleFlowReport>> GetVehicleFlowAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        if (to < from)
            return Result<VehicleFlowReport>.Fail("'to' must be after 'from'.", ReportErrorCodes.ValidationFailed);

        var checkInFilter = Builders<ParkingSessionReadModel>.Filter.And(
            Builders<ParkingSessionReadModel>.Filter.Gte(x => x.CheckInTime, from),
            Builders<ParkingSessionReadModel>.Filter.Lt(x => x.CheckInTime, to));
        var checkInSessions = await _db.ParkingSessions.Find(checkInFilter).ToListAsync(ct);

        var checkOutFilter = Builders<ParkingSessionReadModel>.Filter.And(
            Builders<ParkingSessionReadModel>.Filter.Gte(x => x.CheckOutTime, from),
            Builders<ParkingSessionReadModel>.Filter.Lt(x => x.CheckOutTime, to));
        var checkOutSessions = await _db.ParkingSessions.Find(checkOutFilter).ToListAsync(ct);

        // Khung giờ cao điểm: gom theo giờ-trong-ngày (giờ địa phương GMT+7).
        var peakHours = Enumerable.Range(0, 24)
            .Select(h => new HourlyFlowBucket
            {
                Hour = h,
                CheckIns = checkInSessions.Count(s => ToLocalHour(s.CheckInTime) == h),
                CheckOuts = checkOutSessions.Count(s => s.CheckOutTime.HasValue && ToLocalHour(s.CheckOutTime.Value) == h)
            })
            .ToList();

        // Thống kê theo loại phương tiện (gộp cả lượt vào và lượt ra).
        var byVehicleType = checkInSessions.Select(s => s.VehicleTypeId)
            .Concat(checkOutSessions.Select(s => s.VehicleTypeId))
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct()
            .Select(id => new VehicleTypeFlow
            {
                VehicleTypeId = id,
                CheckIns = checkInSessions.Count(s => s.VehicleTypeId == id),
                CheckOuts = checkOutSessions.Count(s => s.VehicleTypeId == id)
            })
            .OrderByDescending(v => v.CheckIns + v.CheckOuts)
            .ToList();

        return Result<VehicleFlowReport>.Ok(new VehicleFlowReport
        {
            From = from,
            To = to,
            CheckIns = checkInSessions.Count,
            CheckOuts = checkOutSessions.Count,
            PeakHours = peakHours,
            ByVehicleType = byVehicleType
        });
    }

    public async Task<Result<SubscriptionReport>> GetSubscriptionsAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        if (to < from)
            return Result<SubscriptionReport>.Fail("'to' must be after 'from'.", ReportErrorCodes.ValidationFailed);

        var active = await _db.Subscriptions.Find(x => x.Status == SubscriptionActive).ToListAsync(ct);
        var expired = (int)await _db.Subscriptions.CountDocumentsAsync(x => x.Status == SubscriptionExpired, cancellationToken: ct);

        // Active subscriptions whose EndDate falls within [from, to] are "expiring".
        var expiring = active.Count(x => x.EndDate >= from && x.EndDate < to);

        return Result<SubscriptionReport>.Ok(new SubscriptionReport
        {
            From = from,
            To = to,
            ActiveSubscriptions = active.Count,
            ExpiringSubscriptions = expiring,
            ExpiredSubscriptions = expired,
            MonthlyRecurringRevenue = active.Sum(x => x.MonthlyFee)
        });
    }

    public async Task<Result<List<ShiftReconciliationReport>>> GetShiftReconciliationAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        if (to < from)
            return Result<List<ShiftReconciliationReport>>.Fail("'to' must be after 'from'.", ReportErrorCodes.ValidationFailed);

        var filter = Builders<ShiftReadModel>.Filter.And(
            Builders<ShiftReadModel>.Filter.Gte(x => x.OpenedAt, from),
            Builders<ShiftReadModel>.Filter.Lt(x => x.OpenedAt, to));

        var shifts = await _db.Shifts.Find(filter).SortByDescending(x => x.OpenedAt).ToListAsync(ct);

        var report = shifts.Select(s => new ShiftReconciliationReport
        {
            ShiftId = s.Id,
            StaffUserId = s.StaffUserId,
            BuildingId = s.BuildingId,
            OpenedAt = s.OpenedAt,
            ClosedAt = s.ClosedAt,
            ExpectedCashAmount = s.ExpectedCashAmount,
            CountedCashAmount = s.CountedCashAmount,
            DifferenceAmount = s.DifferenceAmount,
            TotalNonCashAmount = s.TotalNonCashAmount,
            TotalPayments = s.TotalPayments
        }).ToList();

        return Result<List<ShiftReconciliationReport>>.Ok(report);
    }

    private async Task<decimal> SumPaidAsync(DateTime from, DateTime to, CancellationToken ct)
    {
        var filter = Builders<PaymentReadModel>.Filter.And(
            Builders<PaymentReadModel>.Filter.Eq(x => x.Status, PaymentPaid),
            Builders<PaymentReadModel>.Filter.Gte(x => x.PaidAt, from),
            Builders<PaymentReadModel>.Filter.Lt(x => x.PaidAt, to));
        var paid = await _db.Payments.Find(filter).ToListAsync(ct);
        return paid.Sum(x => x.Amount);
    }
}
