# Parking Manager

Parking Manager is a parking lot management system for buildings, built as a microservices solution. The system helps staff handle vehicle check-in/check-out, assign parking slots, calculate parking fees, confirm payments, and provide operational reports for facility managers.

## Project Status

This repository contains the backend microservices scaffold using ASP.NET Core and Clean Architecture-style project separation. The current implementation focuses on the service skeleton: each service connects to its own MongoDB Atlas database and exposes health-check endpoints. Business logic (entities, controllers, use cases) is the next step.

## Tech Stack

- ASP.NET Core Web API
- .NET 8
- Clean Architecture (per service)
- Ocelot API Gateway
- MongoDB Atlas (database per service)
- MongoDB.Driver
- Swagger/OpenAPI

## Solution Structure

```txt
PRN232_PRJ/
|-- src/
|   |-- ApiGateway/                 # Ocelot API Gateway
|   |-- Shared/
|   |   |-- Shared.Common/          # Shared settings, utilities
|   |   `-- Shared.Contracts/       # Shared DTOs, cross-service interfaces
|   `-- Services/
|       |-- Auth/                   # Auth.API / Application / Domain / Infrastructure
|       |-- Parking/                # Parking.API / Application / Domain / Infrastructure
|       |-- Payment/                # Payment.API / Application / Domain / Infrastructure
|       `-- Report/                 # Report.API / Application / Domain / Infrastructure
|-- docs/
|-- docker-compose.yml
|-- PRN232_PRJ.sln
`-- README.md
```

Each service follows the same Clean Architecture layering:

- `*.API`: controllers, endpoints, Swagger, request pipeline.
- `*.Application`: use cases, DTOs, service interfaces, validation.
- `*.Domain`: entities, enums, domain rules.
- `*.Infrastructure`: MongoDB context, configuration binding, infrastructure registration.

## Services and Ports

| Service | Port | Database |
|---------|------|----------|
| API Gateway | 5000 | - |
| Auth | 5001 | `parking_auth_db` |
| Parking | 5002 | `parking_main_db` |
| Payment | 5003 | `parking_payment_db` |
| Report | 5004 | `parking_report_db` |

## MVP Features

- Building management
- Vehicle type management
- Floor and zone management
- Parking slot management
- Vehicle check-in / check-out
- Fee calculation
- Payment confirmation
- Monthly subscription
- Shift reconciliation
- Parking session history
- Basic dashboard and reports
- User and role management

## Main Roles

- `Admin`: manages users, roles, and system configuration.
- `FacilityManager`: manages buildings, parking layout, fee policies, and reports.
- `ParkingStaff`: handles vehicle check-in, check-out, slot updates, payments, and parking exceptions.
- `Driver`: views parking information and optional driver-facing features.

## Configuration

Each service reads its MongoDB connection and JWT settings from `appsettings.json` under the `MongoDbSettings` and `JwtSettings` sections. `appsettings.json` is ignored by Git because it holds the real Atlas connection string with a password, so it is never pushed to GitHub.

Expected `appsettings.json` shape per service (Auth shown as example):

```json
{
  "MongoDbSettings": {
    "ConnectionString": "mongodb+srv://<username>:<password>@<cluster-url>/?retryWrites=true&w=majority",
    "DatabaseName": "parking_auth_db"
  },
  "JwtSettings": {
    "Secret": "<at-least-32-character-secret>",
    "Issuer": "ParkingSystemAPI",
    "Audience": "ParkingSystemClient",
    "ExpiryMinutes": 60
  }
}
```

Set `DatabaseName` per service: `parking_auth_db`, `parking_main_db`, `parking_payment_db`, `parking_report_db`.

Because `appsettings.json` is not committed, request the file from the project owner when setting up a new machine. No Docker is required to run the services; only MongoDB Atlas access via the connection string.

## How to Run

Restore and build the solution:

```bash
dotnet restore PRN232_PRJ.sln
dotnet build PRN232_PRJ.sln
```

Run each service in its own terminal:

```bash
dotnet run --project src/Services/Auth/Auth.API --urls "http://localhost:5001"
dotnet run --project src/Services/Parking/Parking.API --urls "http://localhost:5002"
dotnet run --project src/Services/Payment/Payment.API --urls "http://localhost:5003"
dotnet run --project src/Services/Report/Report.API --urls "http://localhost:5004"
dotnet run --project src/ApiGateway --urls "http://localhost:5000"
```

## Health Checks

Each service exposes:

```txt
GET /health        # service is up
GET /health/db     # MongoDB Atlas connection check
```

Swagger is available in development mode at `/swagger` on each service port.

## Documentation

Detailed project documentation is available at:

- [Project Documentation](docs/PROJECT_DOCUMENTATION.md)
- [Microservices Architecture](docs/MICROSERVICES_ARCHITECTURE.md)
- [Architecture Diagram](docs/ARCHITECTURE_DIAGRAM.md)
