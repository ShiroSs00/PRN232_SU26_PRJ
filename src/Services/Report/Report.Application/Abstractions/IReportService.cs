using Report.Application.Common;
using Report.Domain.Models;

namespace Report.Application.Abstractions;

public interface IReportService
{
    Task<Result<DashboardSummary>> GetDashboardAsync(CancellationToken ct = default);

    Task<Result<RevenueReport>> GetRevenueAsync(DateTime from, DateTime to, CancellationToken ct = default);

    Task<Result<OccupancyReport>> GetOccupancyAsync(CancellationToken ct = default);

    Task<Result<VehicleFlowReport>> GetVehicleFlowAsync(DateTime from, DateTime to, CancellationToken ct = default);

    Task<Result<SubscriptionReport>> GetSubscriptionsAsync(DateTime from, DateTime to, CancellationToken ct = default);

    Task<Result<List<ShiftReconciliationReport>>> GetShiftReconciliationAsync(DateTime from, DateTime to, CancellationToken ct = default);
}
