using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Parking.Application.Abstractions;
using Parking.Application.Settings;
using Parking.Infrastructure.Services;
using Shared.Common.Settings;

namespace Parking.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MongoDbSettings>(
            configuration.GetSection(nameof(MongoDbSettings)));
        services.Configure<GeminiSettings>(
            configuration.GetSection(nameof(GeminiSettings)));

        services.AddSingleton<Persistence.MongoDbContext>();
        services.AddSingleton<Persistence.MongoDbInitializer>();

        // Master data
        services.AddScoped<IBuildingService, BuildingService>();
        services.AddScoped<IFloorService, FloorService>();
        services.AddScoped<IZoneService, ZoneService>();
        services.AddScoped<IVehicleTypeService, VehicleTypeService>();
        services.AddScoped<IGateService, GateService>();

        // Slots & map
        services.AddScoped<IParkingSlotService, ParkingSlotService>();
        services.AddScoped<IParkingMapService, ParkingMapService>();

        // Vehicles
        services.AddScoped<IVehicleService, VehicleService>();

        // Operations
        services.AddScoped<IParkingSessionService, ParkingSessionService>();
        services.AddScoped<IReservationService, ReservationService>();
        services.AddScoped<IFeedbackService, FeedbackService>();

        // Tối ưu phân bổ slot + AI
        services.AddScoped<ISlotAllocationService, SlotAllocationService>();
        services.AddScoped<IOptimizationService, OptimizationService>();
        services.AddHttpClient<IGeminiClient, GeminiClient>();

        return services;
    }
}
