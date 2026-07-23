# Parking Manager

Parking Manager is a parking lot management system for buildings, built as a microservices solution. The system helps staff handle vehicle check-in/check-out, assign parking slots, calculate parking fees, confirm payments, and provide operational reports for facility managers.

## Project Status

The backend implements the main parking operations: authenticated vehicle check-in/check-out, server-side fee and monthly-subscription decisions, PayOS reconciliation, payment idempotency, atomic slot/capacity updates, and staff shift reconciliation. Each service owns its MongoDB database and exposes health-check endpoints.

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

Each service reads its MongoDB connection and JWT settings from the standard ASP.NET Core configuration pipeline. Real credentials belong in ignored `appsettings.json` / `appsettings.Local.json` files or environment variables; they must never be committed.
Payment Service also reads `ParkingServiceSettings:BaseUrl` (default `http://localhost:5002`) to validate that a payment's `ShiftId` is the requesting staff member's current open shift.

Copy the matching `appsettings.example.json` file to `appsettings.Local.json` for local development, then replace the placeholders. Environment variables use ASP.NET Core's double-underscore convention, for example `MongoDbSettings__ConnectionString` and `JwtSettings__Secret`.

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


If a credential has ever been committed or shared, remove it from the repository and rotate it at the provider; deleting it in a later commit does not invalidate the exposed value. No Docker is required to run the services; only MongoDB Atlas access via the connection string.

## Core Flows

1. Staff opens a shift with `POST /api/v1/shifts/open`.
2. Check-in resolves a valid monthly subscription on the server, atomically claims zone capacity and a slot, then creates an active session.
3. Checkout is two-stage: `POST /api/v1/parking-sessions/{id}/check-out` calculates the amount and creates an idempotent payment; `POST /api/v1/parking-sessions/{id}/finalize-check-out` requires a matching `Paid` payment before releasing the slot.
4. Cash payments must reference the staff member's current open shift. Closing a shift locks it, rejects pending payments, sums paid cash/non-cash payments, and records any cash difference.
5. PayOS webhook and polling both use the same amount/identity/status reconciliation rules.

Monthly sessions close without a payment when there is no overtime or penalty. A penalty-only monthly fee still requires payment before finalization.

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

Run the automated tests:

```bash
dotnet test PRN232_PRJ.sln --no-restore
```

The suite contains application-level flow integration tests for checkout/payment, monthly eligibility, PayOS reconciliation, and shift reconciliation. Database-backed end-to-end tests require running MongoDB and the services with local configuration.

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
