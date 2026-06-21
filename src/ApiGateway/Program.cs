using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Shared.Common.Settings;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Configuration.AddJsonFile(
    "appsettings.Local.json",
    optional: true,
    reloadOnChange: true);

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: false);

var jwtSettings = builder.Configuration.GetSection(nameof(JwtSettings)).Get<JwtSettings>()
    ?? new JwtSettings();

builder.Services
    .AddAuthentication()
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.Secret))
        };
    });

builder.Services.AddOcelot(builder.Configuration);

// CORS cho frontend (Vite dev server). Cho phép credentials + mọi header/method.
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

app.Map("/health", branch =>
    branch.Run(async context =>
        await context.Response.WriteAsJsonAsync(
            new { status = "ok", service = "ApiGateway" })));

// CORS phải đứng trước Ocelot để xử lý preflight (OPTIONS) trước khi route.
app.UseCors(FrontendCors);

// Bật WebSockets để Ocelot proxy được SignalR hub (parking-map realtime).
app.UseWebSockets();

await app.UseOcelot();

app.Run();
