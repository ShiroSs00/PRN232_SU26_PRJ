namespace Parking.Application.DTOs.Vehicles;

public class VehicleDto
{
    public string Id { get; set; } = string.Empty;

    public string PlateNumber { get; set; } = string.Empty;

    public string PlateNumberNormalized { get; set; } = string.Empty;

    public string VehicleTypeId { get; set; } = string.Empty;

    public string? OwnerUserId { get; set; }

    public string? OwnerName { get; set; }

    public string? OwnerPhone { get; set; }

    public string? OwnerEmail { get; set; }

    public string? Brand { get; set; }

    public string? Model { get; set; }

    public string? Color { get; set; }

    public string? ActiveSubscriptionId { get; set; }

    public string? Note { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

public class CreateVehicleRequest
{
    public string PlateNumber { get; set; } = string.Empty;

    public string VehicleTypeId { get; set; } = string.Empty;

    public string? OwnerUserId { get; set; }

    public string? OwnerName { get; set; }

    public string? OwnerPhone { get; set; }

    public string? OwnerEmail { get; set; }

    public string? Brand { get; set; }

    public string? Model { get; set; }

    public string? Color { get; set; }

    public string? Note { get; set; }
}

public class UpdateVehicleRequest
{
    public string VehicleTypeId { get; set; } = string.Empty;

    public string? OwnerUserId { get; set; }

    public string? OwnerName { get; set; }

    public string? OwnerPhone { get; set; }

    public string? OwnerEmail { get; set; }

    public string? Brand { get; set; }

    public string? Model { get; set; }

    public string? Color { get; set; }

    public string? Note { get; set; }

    public bool IsActive { get; set; }
}
