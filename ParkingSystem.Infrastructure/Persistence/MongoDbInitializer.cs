using MongoDB.Bson;
using MongoDB.Driver;
using ParkingSystem.Domain.Constants;
using ParkingSystem.Domain.Entities;
using ParkingSystem.Application.Services;

namespace ParkingSystem.Infrastructure.Persistence;

public class MongoDbInitializer
{
    private readonly MongoDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public MongoDbInitializer(MongoDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task InitializeAsync()
    {
        await CreateIndexesAsync();
        await SeedDataAsync();
    }

    private async Task CreateIndexesAsync()
    {
        await _context.Users.Indexes.CreateOneAsync(
            new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(user => user.Email),
                new CreateIndexOptions { Unique = true }));

        await _context.Roles.Indexes.CreateOneAsync(
            new CreateIndexModel<Role>(
                Builders<Role>.IndexKeys.Ascending(role => role.Name),
                new CreateIndexOptions { Unique = true }));

        await _context.ParkingSlots.Indexes.CreateOneAsync(
            new CreateIndexModel<ParkingSlot>(
                Builders<ParkingSlot>.IndexKeys.Ascending(slot => slot.Code),
                new CreateIndexOptions { Unique = true }));

        await _context.ParkingSessions.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<ParkingSession>(
                Builders<ParkingSession>.IndexKeys.Ascending(session => session.PlateNumber)),
            new CreateIndexModel<ParkingSession>(
                Builders<ParkingSession>.IndexKeys.Ascending(session => session.Status))
        ]);

        await _context.ParkingSlots.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<ParkingSlot>(
                Builders<ParkingSlot>.IndexKeys.Ascending(slot => slot.Status)),
            new CreateIndexModel<ParkingSlot>(
                Builders<ParkingSlot>.IndexKeys.Ascending(slot => slot.BuildingId))
        ]);

        await _context.Zones.Indexes.CreateOneAsync(
            new CreateIndexModel<Zone>(
                Builders<Zone>.IndexKeys.Ascending(zone => zone.BuildingId)));

        await _context.Floors.Indexes.CreateOneAsync(
            new CreateIndexModel<Floor>(
                Builders<Floor>.IndexKeys.Ascending(floor => floor.BuildingId)));
    }

    private async Task SeedDataAsync()
    {
        await SeedRolesAsync();

        var building = await GetOrCreateBuildingAsync();
        var motorcycle = await GetOrCreateVehicleTypeAsync("Motorcycle", "Motorcycle parking type");
        var car = await GetOrCreateVehicleTypeAsync("Car", "Car parking type");

        var floorB1 = await GetOrCreateFloorAsync(building.Id, "B1", "B1");
        var floorB2 = await GetOrCreateFloorAsync(building.Id, "B2", "B2");

        var motorcycleZone = await GetOrCreateZoneAsync(
            building.Id,
            floorB1.Id,
            motorcycle.Id,
            "B1-Motorcycle-Zone",
            24);

        var carZone = await GetOrCreateZoneAsync(
            building.Id,
            floorB2.Id,
            car.Id,
            "B2-Car-Zone",
            24);

        await SeedParkingSlotsAsync(building.Id, floorB1.Id, motorcycleZone.Id, motorcycle.Id, "B1-M", 24);
        await SeedParkingSlotsAsync(building.Id, floorB2.Id, carZone.Id, car.Id, "B2-C", 24);
        await SeedFeePolicyAsync(motorcycle.Id, "Motorcycle Hourly", 5000m, 3000m);
        await SeedFeePolicyAsync(car.Id, "Car Hourly", 20000m, 10000m);
        await SeedUsersAsync();
    }

    private async Task SeedUsersAsync()
    {
        if (await _context.Users.CountDocumentsAsync(Builders<User>.Filter.Empty) > 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var users = new[]
        {
            new User
            {
                Id = ObjectId.GenerateNewId().ToString(),
                FullName = "System Administrator",
                Email = "admin@parking.com",
                PasswordHash = _passwordHasher.HashPassword("Admin@123"),
                PhoneNumber = "0123456789",
                Roles = new List<string> { UserRoles.Admin },
                IsActive = true,
                CreatedAt = now
            },
            new User
            {
                Id = ObjectId.GenerateNewId().ToString(),
                FullName = "Facility Manager",
                Email = "manager@parking.com",
                PasswordHash = _passwordHasher.HashPassword("Manager@123"),
                PhoneNumber = "0123456788",
                Roles = new List<string> { UserRoles.FacilityManager },
                IsActive = true,
                CreatedAt = now
            },
            new User
            {
                Id = ObjectId.GenerateNewId().ToString(),
                FullName = "Parking Staff",
                Email = "staff@parking.com",
                PasswordHash = _passwordHasher.HashPassword("Staff@123"),
                PhoneNumber = "0123456787",
                Roles = new List<string> { UserRoles.ParkingStaff },
                IsActive = true,
                CreatedAt = now
            }
        };

        await _context.Users.InsertManyAsync(users);
    }

    private async Task SeedRolesAsync()
    {
        if (await _context.Roles.CountDocumentsAsync(Builders<Role>.Filter.Empty) > 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var roles = new[]
        {
            new Role
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Name = UserRoles.Admin,
                Description = "System administrator",
                CreatedAt = now
            },
            new Role
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Name = UserRoles.FacilityManager,
                Description = "Parking facility manager",
                CreatedAt = now
            },
            new Role
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Name = UserRoles.ParkingStaff,
                Description = "Parking staff",
                CreatedAt = now
            },
            new Role
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Name = UserRoles.Driver,
                Description = "Parking driver",
                CreatedAt = now
            }
        };

        await _context.Roles.InsertManyAsync(roles);
    }

    private async Task<Building> GetOrCreateBuildingAsync()
    {
        var building = await _context.Buildings
            .Find(item => item.Name == "Main Building Parking")
            .FirstOrDefaultAsync();

        if (building is not null)
        {
            return building;
        }

        building = new Building
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Name = "Main Building Parking",
            Address = "FPT University Parking Area",
            OpeningTime = "06:00",
            ClosingTime = "23:00",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Buildings.InsertOneAsync(building);
        return building;
    }

    private async Task<VehicleType> GetOrCreateVehicleTypeAsync(string name, string description)
    {
        var vehicleType = await _context.VehicleTypes
            .Find(item => item.Name == name)
            .FirstOrDefaultAsync();

        if (vehicleType is not null)
        {
            return vehicleType;
        }

        vehicleType = new VehicleType
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Name = name,
            Description = description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _context.VehicleTypes.InsertOneAsync(vehicleType);
        return vehicleType;
    }

    private async Task<Floor> GetOrCreateFloorAsync(string buildingId, string floorNumber, string name)
    {
        var floor = await _context.Floors
            .Find(item => item.BuildingId == buildingId && item.FloorNumber == floorNumber)
            .FirstOrDefaultAsync();

        if (floor is not null)
        {
            return floor;
        }

        floor = new Floor
        {
            Id = ObjectId.GenerateNewId().ToString(),
            BuildingId = buildingId,
            FloorNumber = floorNumber,
            Name = name,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Floors.InsertOneAsync(floor);
        return floor;
    }

    private async Task<Zone> GetOrCreateZoneAsync(
        string buildingId,
        string floorId,
        string vehicleTypeId,
        string name,
        int capacity)
    {
        var zone = await _context.Zones
            .Find(item => item.BuildingId == buildingId && item.Name == name)
            .FirstOrDefaultAsync();

        if (zone is not null)
        {
            return zone;
        }

        zone = new Zone
        {
            Id = ObjectId.GenerateNewId().ToString(),
            BuildingId = buildingId,
            FloorId = floorId,
            VehicleTypeId = vehicleTypeId,
            Name = name,
            Capacity = capacity,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Zones.InsertOneAsync(zone);
        return zone;
    }

    private async Task SeedParkingSlotsAsync(
        string buildingId,
        string floorId,
        string zoneId,
        string vehicleTypeId,
        string prefix,
        int count)
    {
        var existingCount = await _context.ParkingSlots.CountDocumentsAsync(
            slot => slot.ZoneId == zoneId);

        if (existingCount > 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var slots = Enumerable.Range(1, count)
            .Select(index => {
                var isCar = prefix.Contains("-C");
                var width = isCar ? 120.0 : 80.0;
                var height = isCar ? 180.0 : 100.0;
                var colsPerRow = 6;
                var row = (index - 1) / colsPerRow + 1;
                var col = (index - 1) % colsPerRow + 1;
                
                return new ParkingSlot
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    BuildingId = buildingId,
                    FloorId = floorId,
                    ZoneId = zoneId,
                    VehicleTypeId = vehicleTypeId,
                    Code = $"{prefix}-{index:000}",
                    Status = ParkingSlotStatuses.Available,
                    Row = row,
                    Column = col,
                    PositionX = 50.0 + (col - 1) * width,
                    PositionY = 50.0 + (row - 1) * height,
                    Width = width,
                    Height = height,
                    IsActive = true,
                    CreatedAt = now
                };
            })
            .ToList();

        await _context.ParkingSlots.InsertManyAsync(slots);
    }

    private async Task SeedFeePolicyAsync(
        string vehicleTypeId,
        string name,
        decimal basePrice,
        decimal hourlyPrice)
    {
        var exists = await _context.FeePolicies.Find(policy => policy.Name == name).AnyAsync();
        if (exists)
        {
            return;
        }

        var feePolicy = new FeePolicy
        {
            Id = ObjectId.GenerateNewId().ToString(),
            VehicleTypeId = vehicleTypeId,
            Name = name,
            PricingType = PricingTypes.Hourly,
            BasePrice = basePrice,
            HourlyPrice = hourlyPrice,
            IsActive = true,
            EffectiveFrom = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        await _context.FeePolicies.InsertOneAsync(feePolicy);
    }
}
