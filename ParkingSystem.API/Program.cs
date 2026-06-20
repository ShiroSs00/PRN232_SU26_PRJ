using Microsoft.AspNetCore.Mvc;
using ParkingSystem.API.Extensions;
using ParkingSystem.API.Filters;
using ParkingSystem.API.Middlewares;
using ParkingSystem.Infrastructure;
using ParkingSystem.Infrastructure.Persistence;

namespace ParkingSystem.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Configuration.AddJsonFile(
                "appsettings.Local.json",
                optional: true,
                reloadOnChange: true);

            builder.Services.AddControllers(options =>
            {
                options.Filters.Add<ValidationFilter>();
            });

            builder.Services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerDocumentation();
            builder.Services.AddInfrastructure(builder.Configuration);
            builder.Services.AddCustomCors();

            var app = builder.Build();

            // Register global exception handling middleware first
            app.UseMiddleware<ExceptionMiddleware>();

            using (var scope = app.Services.CreateScope())
            {
                var initializer = scope.ServiceProvider.GetRequiredService<MongoDbInitializer>();
                await initializer.InitializeAsync();
            }

            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseHttpsRedirection();

            app.UseCors("ReactFrontend");

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.MapHub<ParkingSystem.API.Hubs.ParkingMapHub>("/hubs/parking-map");

            app.Run();
        }
    }
}
