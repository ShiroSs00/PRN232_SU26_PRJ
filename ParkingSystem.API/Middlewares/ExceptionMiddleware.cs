using System.Net;
using System.Text.Json;
using ParkingSystem.Application.Common;

namespace ParkingSystem.API.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var statusCode = HttpStatusCode.InternalServerError;
        var errorMessage = "An unexpected error occurred on the server.";

        if (exception is UnauthorizedAccessException)
        {
            statusCode = HttpStatusCode.Forbidden;
            errorMessage = exception.Message;
        }
        else if (_env.IsDevelopment())
        {
            errorMessage = exception.Message;
        }

        context.Response.StatusCode = (int)statusCode;
        var errors = _env.IsDevelopment() ? exception.StackTrace : null;

        var response = ApiResponse.Fail(errorMessage, errors);

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(response, options);

        await context.Response.WriteAsync(json);
    }
}
