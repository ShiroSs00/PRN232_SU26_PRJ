using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using ParkingSystem.Application.Common;
using ParkingSystem.Domain.Entities;
using ParkingSystem.Infrastructure.Persistence;

namespace ParkingSystem.API.Controllers;

[ApiController]
[Route("api/v1/database-test")]
public class DatabaseTestController : ControllerBase
{
    private readonly MongoDbContext _context;
    private readonly MongoDbInitializer _initializer;

    public DatabaseTestController(MongoDbContext context, MongoDbInitializer initializer)
    {
        _context = context;
        _initializer = initializer;
    }

    [HttpGet("health")]
    public async Task<ActionResult<ApiResponse>> Health()
    {
        try
        {
            await _context.Database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));

            return Ok(ApiResponse.Ok(
                "MongoDB connected successfully",
                new
                {
                    databaseName = _context.DatabaseName
                }));
        }
        catch (Exception ex)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                ApiResponse.Fail(
                    "MongoDB connection failed",
                    new
                    {
                        errorType = ex.GetType().Name,
                        ex.Message
                    }));
        }
    }

    [HttpGet("seed-summary")]
    public async Task<ActionResult<ApiResponse>> SeedSummary()
    {
        try
        {
            var data = new
            {
                roles = await _context.Roles.CountDocumentsAsync(Builders<Role>.Filter.Empty),
                buildings = await _context.Buildings.CountDocumentsAsync(Builders<Building>.Filter.Empty),
                vehicleTypes = await _context.VehicleTypes.CountDocumentsAsync(Builders<VehicleType>.Filter.Empty),
                floors = await _context.Floors.CountDocumentsAsync(Builders<Floor>.Filter.Empty),
                zones = await _context.Zones.CountDocumentsAsync(Builders<Zone>.Filter.Empty),
                parkingSlots = await _context.ParkingSlots.CountDocumentsAsync(Builders<ParkingSlot>.Filter.Empty),
                feePolicies = await _context.FeePolicies.CountDocumentsAsync(Builders<FeePolicy>.Filter.Empty)
            };

            return Ok(ApiResponse.Ok("Seed summary loaded successfully", data));
        }
        catch (Exception ex)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                ApiResponse.Fail(
                    "MongoDB seed summary failed",
                    new
                    {
                        errorType = ex.GetType().Name,
                        ex.Message
                    }));
        }
    }

    [HttpPost("initialize")]
    public async Task<ActionResult<ApiResponse>> Initialize()
    {
        try
        {
            await _initializer.InitializeAsync();

            return Ok(ApiResponse.Ok(
                "MongoDB indexes and seed data initialized successfully",
                new
                {
                    databaseName = _context.DatabaseName
                }));
        }
        catch (Exception ex)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                ApiResponse.Fail(
                    "MongoDB initialization failed",
                    new
                    {
                        errorType = ex.GetType().Name,
                        ex.Message
                    }));
        }
    }
}
