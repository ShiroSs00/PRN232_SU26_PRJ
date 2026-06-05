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

    public DatabaseTestController(MongoDbContext context)
    {
        _context = context;
    }

    [HttpGet("health")]
    public async Task<ActionResult<ApiResponse>> Health()
    {
        await _context.Database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));

        return Ok(ApiResponse.Ok(
            "MongoDB connected successfully",
            new
            {
                databaseName = _context.DatabaseName
            }));
    }

    [HttpGet("seed-summary")]
    public async Task<ActionResult<ApiResponse>> SeedSummary()
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
}
