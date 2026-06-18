using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Report.Application.Abstractions;
using Report.Application.Settings;
using Report.Infrastructure.Services;
using Shared.Common.Settings;

namespace Report.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MongoDbSettings>(
            configuration.GetSection(nameof(MongoDbSettings)));

        services.Configure<ReportSourceSettings>(
            configuration.GetSection(nameof(ReportSourceSettings)));

        services.AddSingleton<Persistence.MongoDbContext>();
        services.AddSingleton<Persistence.MongoDbInitializer>();

        services.AddScoped<IReportService, ReportService>();

        return services;
    }
}
