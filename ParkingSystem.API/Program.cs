
using ParkingSystem.Infrastructure;
using ParkingSystem.Infrastructure.Persistence;

namespace ParkingSystem.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddInfrastructure(builder.Configuration);
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("ReactFrontend", policy =>
                {
                    policy
                        .WithOrigins("http://localhost:5173")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var initializer = scope.ServiceProvider.GetRequiredService<MongoDbInitializer>();
                await initializer.InitializeAsync();
            }

            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseHttpsRedirection();

            app.UseCors("ReactFrontend");

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
