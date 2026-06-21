using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Bson;
using Parking.API.Hubs;
using Parking.API.Realtime;
using Parking.Application.Abstractions;
using Parking.Infrastructure;
using Parking.Infrastructure.Persistence;
using Shared.Common.Settings;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile(
    "appsettings.Local.json",
    optional: true,
    reloadOnChange: true);

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Parking API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddInfrastructure(builder.Configuration);

// SignalR notifier — đặt ở tầng API vì chỉ Web SDK mới có IHubContext.
builder.Services.AddScoped<IParkingMapNotifier, SignalRParkingMapNotifier>();

var jwtSettings = builder.Configuration.GetSection(nameof(JwtSettings)).Get<JwtSettings>()
    ?? throw new InvalidOperationException("JwtSettings configuration is missing.");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = System.Security.Claims.ClaimTypes.Name,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };

        // SignalR gửi token qua query string (?access_token=) vì WebSocket
        // không đặt được header Authorization. Đọc token cho request tới hub.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/hubs/parking-map"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// CORS cho frontend (SignalR cần origin cụ thể + AllowCredentials).
const string FrontendCors = "FrontendCors";
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCors, policy =>
        policy
            .SetIsOriginAllowed(origin =>
            {
                if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)) return false;

                return uri.Scheme is "http" or "https"
                    && (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                        || uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
                        || uri.Host.Equals("10.0.2.2", StringComparison.OrdinalIgnoreCase));
            })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

var app = builder.Build();

await app.Services.GetRequiredService<MongoDbInitializer>().InitializeAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(FrontendCors);

app.UseAuthentication();
app.UseAuthorization();

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
app.MapHub<ParkingMapHub>("/hubs/parking-map");

app.Run();
