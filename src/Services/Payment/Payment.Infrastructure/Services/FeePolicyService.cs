using MongoDB.Bson;
using MongoDB.Driver;
using Payment.Application.Abstractions;
using Payment.Application.Common;
using Payment.Application.DTOs;
using Payment.Application.DTOs.FeePolicies;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Infrastructure.Persistence;

namespace Payment.Infrastructure.Services;

public class FeePolicyService : IFeePolicyService
{
    private readonly MongoDbContext _db;

    public FeePolicyService(MongoDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PagedResult<FeePolicyDto>>> GetListAsync(
        string? buildingId,
        string? vehicleTypeId,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 200) pageSize = 200;

        var fb = Builders<FeePolicy>.Filter;
        var filters = new List<FilterDefinition<FeePolicy>>();
        if (!string.IsNullOrWhiteSpace(buildingId)) filters.Add(fb.Eq(x => x.BuildingId, buildingId));
        if (!string.IsNullOrWhiteSpace(vehicleTypeId)) filters.Add(fb.Eq(x => x.VehicleTypeId, vehicleTypeId));
        if (isActive.HasValue) filters.Add(fb.Eq(x => x.IsActive, isActive.Value));

        var filter = filters.Count == 0 ? fb.Empty : fb.And(filters);
        var total = await _db.FeePolicies.CountDocumentsAsync(filter, cancellationToken: ct);

        var items = await _db.FeePolicies.Find(filter)
            .SortByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(ct);

        return Result<PagedResult<FeePolicyDto>>.Ok(new PagedResult<FeePolicyDto>
        {
            Items = items.Select(Map).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        });
    }

    public async Task<Result<FeePolicyDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var entity = await _db.FeePolicies.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (entity is null)
            return Result<FeePolicyDto>.Fail("Fee policy not found.", PaymentErrorCodes.FeePolicyNotFound);
        return Result<FeePolicyDto>.Ok(Map(entity));
    }

    public async Task<Result<List<FeePolicyDto>>> GetActiveAsync(
        string? buildingId,
        string? vehicleTypeId,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var fb = Builders<FeePolicy>.Filter;
        var filters = new List<FilterDefinition<FeePolicy>>
        {
            fb.Eq(x => x.IsActive, true),
            fb.Lte(x => x.EffectiveFrom, now),
            fb.Or(fb.Eq(x => x.EffectiveTo, null), fb.Gte(x => x.EffectiveTo, now))
        };
        if (!string.IsNullOrWhiteSpace(buildingId)) filters.Add(fb.Eq(x => x.BuildingId, buildingId));
        if (!string.IsNullOrWhiteSpace(vehicleTypeId)) filters.Add(fb.Eq(x => x.VehicleTypeId, vehicleTypeId));

        var items = await _db.FeePolicies.Find(fb.And(filters))
            .SortByDescending(x => x.EffectiveFrom)
            .ToListAsync(ct);
        return Result<List<FeePolicyDto>>.Ok(items.Select(Map).ToList());
    }

    public async Task<Result<FeePolicyDto>> CreateAsync(CreateFeePolicyRequest request, CancellationToken ct = default)
    {
        var validation = ValidateCreate(request);
        if (validation is not null)
            return Result<FeePolicyDto>.Fail(validation, PaymentErrorCodes.ValidationFailed);

        var now = DateTime.UtcNow;
        var entity = new FeePolicy
        {
            Id = ObjectId.GenerateNewId().ToString(),
            BuildingId = request.BuildingId.Trim(),
            VehicleTypeId = request.VehicleTypeId.Trim(),
            Name = request.Name.Trim(),
            PricingType = request.PricingType,
            BasePrice = request.BasePrice,
            HourlyPrice = request.HourlyPrice,
            DailyPrice = request.DailyPrice,
            MonthlyPrice = request.MonthlyPrice,
            LostTicketFee = request.LostTicketFee,
            OvertimeFee = request.OvertimeFee,
            OvertimeAfterHours = request.OvertimeAfterHours,
            EffectiveFrom = request.EffectiveFrom ?? now,
            EffectiveTo = request.EffectiveTo,
            CreatedAt = now,
            IsActive = true
        };

        await _db.FeePolicies.InsertOneAsync(entity, cancellationToken: ct);
        return Result<FeePolicyDto>.Ok(Map(entity));
    }

    public async Task<Result<FeePolicyDto>> UpdateAsync(string id, UpdateFeePolicyRequest request, CancellationToken ct = default)
    {
        var entity = await _db.FeePolicies.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (entity is null)
            return Result<FeePolicyDto>.Fail("Fee policy not found.", PaymentErrorCodes.FeePolicyNotFound);

        var update = Builders<FeePolicy>.Update
            .Set(x => x.Name, request.Name.Trim())
            .Set(x => x.PricingType, request.PricingType)
            .Set(x => x.BasePrice, request.BasePrice)
            .Set(x => x.HourlyPrice, request.HourlyPrice)
            .Set(x => x.DailyPrice, request.DailyPrice)
            .Set(x => x.MonthlyPrice, request.MonthlyPrice)
            .Set(x => x.LostTicketFee, request.LostTicketFee)
            .Set(x => x.OvertimeFee, request.OvertimeFee)
            .Set(x => x.OvertimeAfterHours, request.OvertimeAfterHours)
            .Set(x => x.EffectiveFrom, request.EffectiveFrom)
            .Set(x => x.EffectiveTo, request.EffectiveTo)
            .Set(x => x.IsActive, request.IsActive)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        await _db.FeePolicies.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);
        var updated = await _db.FeePolicies.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        return Result<FeePolicyDto>.Ok(Map(updated!));
    }

    public async Task<Result> DeleteAsync(string id, CancellationToken ct = default)
    {
        var entity = await _db.FeePolicies.Find(x => x.Id == id).FirstOrDefaultAsync(ct);
        if (entity is null)
            return Result.Fail("Fee policy not found.", PaymentErrorCodes.FeePolicyNotFound);

        var update = Builders<FeePolicy>.Update
            .Set(x => x.IsActive, false)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);
        await _db.FeePolicies.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);
        return Result.Ok();
    }

    public async Task<Result<CalculateFeeResponse>> CalculateAsync(CalculateFeeRequest request, CancellationToken ct = default)
    {
        if (request.CheckOutTime < request.CheckInTime)
            return Result<CalculateFeeResponse>.Fail("Check-out time must be after check-in time.", PaymentErrorCodes.ValidationFailed);

        var now = DateTime.UtcNow;
        var fb = Builders<FeePolicy>.Filter;
        var filter = fb.And(
            fb.Eq(x => x.BuildingId, request.BuildingId),
            fb.Eq(x => x.VehicleTypeId, request.VehicleTypeId),
            fb.Eq(x => x.IsActive, true),
            fb.Lte(x => x.EffectiveFrom, request.CheckInTime),
            fb.Or(fb.Eq(x => x.EffectiveTo, null), fb.Gte(x => x.EffectiveTo, request.CheckInTime)));

        var policy = await _db.FeePolicies.Find(filter)
            .SortByDescending(x => x.EffectiveFrom)
            .FirstOrDefaultAsync(ct);

        if (policy is null)
            return Result<CalculateFeeResponse>.Fail(
                "No active fee policy matches the given building/vehicle type.",
                PaymentErrorCodes.ActivePolicyNotFound);

        var duration = request.CheckOutTime - request.CheckInTime;
        var breakdown = new List<FeeBreakdownItem>();
        decimal amount = 0;

        if (FeeCalculationRules.IncludeBaseCharge(request.PenaltiesOnly))
        {
            switch (policy.PricingType)
            {
                case PricingType.PerTurn:
                    amount = policy.BasePrice;
                    breakdown.Add(new FeeBreakdownItem { Description = "Per-turn base", Amount = policy.BasePrice });
                    break;

                case PricingType.Hourly:
                    {
                        var hours = (int)Math.Ceiling(duration.TotalHours);
                        if (hours < 1) hours = 1;
                        amount = policy.BasePrice;
                        breakdown.Add(new FeeBreakdownItem { Description = "Base price", Amount = policy.BasePrice });

                        var hourlyRate = policy.HourlyPrice ?? 0m;
                        if (hourlyRate > 0)
                        {
                            var hourlyTotal = hourlyRate * hours;
                            amount += hourlyTotal;
                            breakdown.Add(new FeeBreakdownItem
                            {
                                Description = $"Hourly ({hours}h x {hourlyRate:0.##})",
                                Amount = hourlyTotal
                            });
                        }
                        break;
                    }

                case PricingType.Daily:
                    {
                        var days = (int)Math.Ceiling(duration.TotalDays);
                        if (days < 1) days = 1;
                        var dailyRate = policy.DailyPrice ?? policy.BasePrice;
                        var dailyTotal = dailyRate * days;
                        amount = dailyTotal;
                        breakdown.Add(new FeeBreakdownItem
                        {
                            Description = $"Daily ({days}d x {dailyRate:0.##})",
                            Amount = dailyTotal
                        });
                        break;
                    }

                case PricingType.Monthly:
                    {
                        amount = policy.MonthlyPrice ?? policy.BasePrice;
                        breakdown.Add(new FeeBreakdownItem { Description = "Monthly fee", Amount = amount });
                        break;
                    }
            }
        }

        // Phụ phí quá giờ cố định 1 lần: áp cho PerTurn và Hourly khi thời gian gửi
        // vượt ngưỡng OvertimeAfterHours (mặc định 24h nếu không cấu hình, giữ hành vi cũ).
        var overtimeThreshold = policy.OvertimeAfterHours ?? 24;
        if (FeeCalculationRules.ShouldApplyOvertime(
                policy.PricingType,
                policy.OvertimeFee,
                policy.OvertimeAfterHours,
                duration,
                request.PenaltiesOnly))
        {
            amount += policy.OvertimeFee;
            breakdown.Add(new FeeBreakdownItem
            {
                Description = $"Phụ phí quá giờ (sau {overtimeThreshold}h)",
                Amount = policy.OvertimeFee
            });
        }

        if (request.IsLostTicket && policy.LostTicketFee > 0)
        {
            amount += policy.LostTicketFee;
            breakdown.Add(new FeeBreakdownItem { Description = "Lost ticket fee", Amount = policy.LostTicketFee });
        }

        return Result<CalculateFeeResponse>.Ok(new CalculateFeeResponse
        {
            Amount = amount,
            FeePolicyId = policy.Id,
            PricingType = policy.PricingType,
            Duration = duration,
            Breakdown = breakdown
        });
    }

    private static string? ValidateCreate(CreateFeePolicyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.BuildingId)) return "BuildingId is required.";
        if (string.IsNullOrWhiteSpace(request.VehicleTypeId)) return "VehicleTypeId is required.";
        if (string.IsNullOrWhiteSpace(request.Name)) return "Name is required.";
        if (request.BasePrice < 0) return "BasePrice must be non-negative.";
        return request.PricingType switch
        {
            PricingType.Hourly when (request.HourlyPrice ?? 0) < 0 => "HourlyPrice must be non-negative.",
            PricingType.Daily when (request.DailyPrice ?? 0) <= 0 => "DailyPrice must be positive for daily pricing.",
            PricingType.Monthly when (request.MonthlyPrice ?? 0) <= 0 => "MonthlyPrice must be positive for monthly pricing.",
            _ => null
        };
    }

    private static FeePolicyDto Map(FeePolicy x) => new()
    {
        Id = x.Id,
        BuildingId = x.BuildingId,
        VehicleTypeId = x.VehicleTypeId,
        Name = x.Name,
        PricingType = x.PricingType,
        BasePrice = x.BasePrice,
        HourlyPrice = x.HourlyPrice,
        DailyPrice = x.DailyPrice,
        MonthlyPrice = x.MonthlyPrice,
        LostTicketFee = x.LostTicketFee,
        OvertimeFee = x.OvertimeFee,
        OvertimeAfterHours = x.OvertimeAfterHours,
        EffectiveFrom = x.EffectiveFrom,
        EffectiveTo = x.EffectiveTo,
        IsActive = x.IsActive,
        CreatedAt = x.CreatedAt,
        UpdatedAt = x.UpdatedAt
    };
}
