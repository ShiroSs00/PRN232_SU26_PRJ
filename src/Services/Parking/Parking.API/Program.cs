using Parking.Infrastructure;
using Parking.Infrastructure.Persistence;
using MongoDB.Bson;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile(
    "appsettings.Local.json",
    optional: true,
    reloadOnChange: true);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

await app.Services.GetRequiredService<MongoDbInitializer>().InitializeAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "Parking" }));

app.MapGet("/health/db", async (MongoDbContext context) =>
{
    try
    {
        await context.Database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));
        return Results.Ok(new { status = "ok", database = context.DatabaseName });
    }
    catch (Exception ex)
    {
        return Results.Json(
            new { status = "error", error = ex.GetType().Name, message = ex.Message },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.MapControllers();

app.Run();
