namespace Parking.Application.Common;

public static class ParkingErrorCodes
{
    // Generic
    public const string ValidationFailed = "VALIDATION_FAILED";

    // Building / Floor / Zone / VehicleType / Gate
    public const string BuildingNotFound = "BUILDING_NOT_FOUND";
    public const string FloorNotFound = "FLOOR_NOT_FOUND";
    public const string ZoneNotFound = "ZONE_NOT_FOUND";
    public const string VehicleTypeNotFound = "VEHICLE_TYPE_NOT_FOUND";
    public const string GateNotFound = "GATE_NOT_FOUND";
    public const string DuplicateVehicleType = "DUPLICATE_VEHICLE_TYPE";
    public const string DuplicateGateCode = "DUPLICATE_GATE_CODE";

    // ParkingSlot
    public const string SlotNotFound = "SLOT_NOT_FOUND";
    public const string DuplicateSlotCode = "DUPLICATE_SLOT_CODE";
    public const string SlotPositionTaken = "SLOT_POSITION_TAKEN";
    public const string SlotNotAvailable = "SLOT_NOT_AVAILABLE";

    // Vehicle
    public const string VehicleNotFound = "VEHICLE_NOT_FOUND";
    public const string DuplicatePlateNumber = "DUPLICATE_PLATE_NUMBER";

    // ParkingSession
    public const string SessionNotFound = "SESSION_NOT_FOUND";
    public const string ActiveSessionExists = "ACTIVE_SESSION_EXISTS";
    public const string SessionNotActive = "SESSION_NOT_ACTIVE";
    public const string NoAvailableSlot = "NO_AVAILABLE_SLOT";
    public const string SessionAccessDenied = "SESSION_ACCESS_DENIED";

    // Incident
    public const string IncidentNotFound = "INCIDENT_NOT_FOUND";
    public const string InvalidIncidentStatus = "INVALID_INCIDENT_STATUS";

    // Reservation
    public const string ReservationNotFound = "RESERVATION_NOT_FOUND";
    public const string InvalidReservationStatus = "INVALID_RESERVATION_STATUS";
    public const string ReservationSlotConflict = "RESERVATION_SLOT_CONFLICT";
    public const string InvalidReservationWindow = "INVALID_RESERVATION_WINDOW";
    public const string ReservationAccessDenied = "RESERVATION_ACCESS_DENIED";
}
