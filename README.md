# Parking Manager

Parking Manager is a parking lot management system for buildings. The system helps staff handle vehicle check-in/check-out, assign parking slots, calculate parking fees, confirm payments, and provide operational reports for facility managers.

## Project Status

This repository currently contains the backend solution scaffold using ASP.NET Core and Clean Architecture-style project separation. The current implementation focuses on MongoDB document models, database context setup, indexes, seed data, and a small database test API.

## Tech Stack

- ASP.NET Core Web API
- .NET 8
- Clean Architecture
- MongoDB Atlas
- MongoDB.Driver
- Swagger/OpenAPI

## Solution Structure

```txt
PRN232_PRJ/
|-- ParkingSystem.API/
|-- ParkingSystem.Application/
|-- ParkingSystem.Domain/
|-- ParkingSystem.Infrastructure/
|-- docs/
|-- PRN232_PRJ.sln
`-- README.md
```

## Layer Responsibilities

- `ParkingSystem.API`: controllers, request/response endpoints, authentication middleware, Swagger configuration.
- `ParkingSystem.Application`: use cases, DTOs, service interfaces, validation, business orchestration.
- `ParkingSystem.Domain`: entities, enums, domain rules, core business concepts.
- `ParkingSystem.Infrastructure`: MongoDB settings, context, indexes, seed data, and infrastructure registration.

## MVP Features

- Building management
- Vehicle type management
- Floor and zone management
- Parking slot management
- Vehicle check-in
- Vehicle check-out
- Fee calculation
- Payment confirmation
- Parking session history
- Basic dashboard and reports
- User and role document models

## Main Roles

- `Admin`: manages users, roles, and system configuration.
- `FacilityManager`: manages buildings, parking layout, fee policies, and reports.
- `ParkingStaff`: handles vehicle check-in, check-out, slot updates, payments, and parking exceptions.
- `Driver`: views parking information and optional driver-facing features.

## Planned API Base URL

```txt
/api/v1
```

Main API modules:

- `/buildings`
- `/vehicle-types`
- `/floors`
- `/zones`
- `/parking-slots`
- `/parking-sessions`
- `/fee-policies`
- `/payments`
- `/reports`
- `/database-test`

## MongoDB Configuration

Keep the sample MongoDB Atlas connection string in `ParkingSystem.API/appsettings.json`. Do not commit a real database password.

```json
{
  "MongoDbSettings": {
    "ConnectionString": "mongodb+srv://<username>:<password>@<cluster-url>/?retryWrites=true&w=majority",
    "DatabaseName": "ParkingManagerDb"
  }
}
```

For local development, create `ParkingSystem.API/appsettings.Local.json`. This file is ignored by Git and can contain your real connection string:

```json
{
  "MongoDbSettings": {
    "ConnectionString": "mongodb+srv://<username>:<password>@<cluster-url>/?retryWrites=true&w=majority&appName=<app-name>",
    "DatabaseName": "ParkingManagerDb"
  }
}
```

## How to Run

Restore and build the solution:

```bash
dotnet restore PRN232_PRJ.sln
dotnet build PRN232_PRJ.sln
```

Run the API:

```bash
dotnet run --project ParkingSystem.API
```

Swagger is available in development mode at:

```txt
https://localhost:7174/swagger
http://localhost:5295/swagger
```

Database test endpoints:

```txt
GET /api/v1/database-test/health
GET /api/v1/database-test/seed-summary
```

## Documentation

Detailed project documentation is available at:

- [Project Documentation](docs/PROJECT_DOCUMENTATION.md)
