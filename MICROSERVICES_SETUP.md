# Parking Manager - Microservices Architecture Setup Guide

## 📋 Tổng quan

Hệ thống Parking Manager được thiết kế theo kiến trúc **Microservices** với **Ocelot API Gateway**.

### Kiến trúc

```
[React Client] 
    ↓
[Ocelot Gateway :5000]
    ↓
    ├─→ [Auth Service :5001]      - Authentication & User Management
    ├─→ [Parking Service :5002]   - Core parking operations
    ├─→ [Payment Service :5003]   - Payment & Fee management
    └─→ [Report Service :5004]    - Analytics & Reports
         ↓
    [MongoDB Database]
```

## 🗂️ Cấu trúc thư mục

```
PRN232_SU26_PRJ/
├── src/
│   ├── ApiGateway/                    # Ocelot API Gateway
│   │   ├── ApiGateway.csproj
│   │   ├── Program.cs
│   │   ├── ocelot.json
│   │   └── ocelot.Development.json
│   │
│   ├── Services/
│   │   ├── Auth/                      # Auth Service
│   │   │   ├── Auth.API/
│   │   │   ├── Auth.Application/
│   │   │   ├── Auth.Domain/
│   │   │   └── Auth.Infrastructure/
│   │   │
│   │   ├── Parking/                   # Parking Service
│   │   │   ├── Parking.API/
│   │   │   ├── Parking.Application/
│   │   │   ├── Parking.Domain/
│   │   │   └── Parking.Infrastructure/
│   │   │
│   │   ├── Payment/                   # Payment Service
│   │   │   ├── Payment.API/
│   │   │   ├── Payment.Application/
│   │   │   ├── Payment.Domain/
│   │   │   └── Payment.Infrastructure/
│   │   │
│   │   └── Report/                    # Report Service
│   │       ├── Report.API/
│   │       ├── Report.Application/
│   │       ├── Report.Domain/
│   │       └── Report.Infrastructure/
│   │
│   └── Shared/                        # Shared libraries
│       ├── Shared.Common/      # Common utilities
│       └── Shared.Contracts/   # Shared DTOs & Interfaces
│
├── docs/
│   ├── PROJECT_DOCUMENTATION.md
│   └── MICROSERVICES_ARCHITECTURE.md
│
├── scripts/
│   ├── run-local.sh
│   └── stop-local.sh
│
├── docker-compose.yml
├── .env.example
└── README.md
```

## 🚀 Bắt đầu

### Prerequisites

- .NET 8 SDK
- Docker Desktop
- MongoDB (hoặc dùng Docker)
- Visual Studio 2022 / Rider / VS Code

### Bước 1: Clone và Setup

```bash
# Clone repository
git clone <repo-url>
cd PRN232_SU26_PRJ

# Checkout microservices branch
git checkout feature/microservices-architecture

# Copy file môi trường
cp .env.example .env
```

### Bước 2: Chạy MongoDB bằng Docker

```bash
docker run -d \
  --name parking-mongodb \
  -p 27017:27017 \
  -e MONGO_INITDB_ROOT_USERNAME=admin \
  -e MONGO_INITDB_ROOT_PASSWORD=password123 \
  mongo:latest
```

### Bước 3: Chạy tất cả services

#### Option A: Dùng Docker Compose (khuyến nghị)

```bash
# Build và chạy tất cả services
docker-compose up --build

# Hoặc chạy background
docker-compose up -d

# Xem logs
docker-compose logs -f

# Dừng tất cả
docker-compose down
```

#### Option B: Chạy thủ công từng service

```bash
# Terminal 1 - API Gateway
cd src/ApiGateway
dotnet run

# Terminal 2 - Auth Service
cd src/Services/Auth/Auth.API
dotnet run --urls="http://localhost:5001"

# Terminal 3 - Parking Service
cd src/Services/Parking/Parking.API
dotnet run --urls="http://localhost:5002"

# Terminal 4 - Payment Service
cd src/Services/Payment/Payment.API
dotnet run --urls="http://localhost:5003"

# Terminal 5 - Report Service
cd src/Services/Report/Report.API
dotnet run --urls="http://localhost:5004"
```

#### Option C: Dùng script

```bash
# Chạy tất cả
chmod +x scripts/run-local.sh
./scripts/run-local.sh

# Dừng tất cả
./scripts/stop-local.sh
```

### Bước 4: Kiểm tra health

```bash
# Gateway
curl http://localhost:5000/health

# Auth Service
curl http://localhost:5001/health

# Parking Service
curl http://localhost:5002/health

# Payment Service
curl http://localhost:5003/health

# Report Service
curl http://localhost:5004/health
```

### Bước 5: Truy cập Swagger

- **Gateway**: http://localhost:5000/swagger
- **Auth Service**: http://localhost:5001/swagger
- **Parking Service**: http://localhost:5002/swagger
- **Payment Service**: http://localhost:5003/swagger
- **Report Service**: http://localhost:5004/swagger

## 📝 Implementation Steps

### Phase 1: Setup Infrastructure (HOÀN THÀNH ✅)

- [x] Tạo cấu trúc folder
- [x] Tạo Ocelot config files
- [x] Tạo Docker Compose
- [x] Tạo run scripts
- [x] Viết documentation

### Phase 2: Shared Libraries

```bash
# 1. Tạo Common library
dotnet new classlib -n Shared.Common -o src/Shared/Shared.Common
dotnet add src/Shared/Shared.Common package Microsoft.Extensions.DependencyInjection.Abstractions

# 2. Tạo Contracts library
dotnet new classlib -n Shared.Contracts -o src/Shared/Shared.Contracts
```

**Nội dung cần implement:**
- `Shared.Common`: BaseEntity, Result pattern, Extensions, Middleware
- `Shared.Contracts`: Shared DTOs, Events, Interfaces

### Phase 3: API Gateway

```bash
# 1. Tạo Gateway project
dotnet new webapi -n ApiGateway -o src/ApiGateway
cd src/ApiGateway

# 2. Add Ocelot package
dotnet add package Ocelot
dotnet add package Ocelot.Cache.CacheManager

# 3. Configure Program.cs (xem file mẫu trong docs/MICROSERVICES_ARCHITECTURE.md)
```

### Phase 4: Auth Service

```bash
# Tạo projects
dotnet new webapi -n Auth.API -o src/Services/Auth/Auth.API
dotnet new classlib -n Auth.Application -o src/Services/Auth/Auth.Application
dotnet new classlib -n Auth.Domain -o src/Services/Auth/Auth.Domain
dotnet new classlib -n Auth.Infrastructure -o src/Services/Auth/Auth.Infrastructure

# Add packages
cd src/Services/Auth/Auth.API
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package Swashbuckle.AspNetCore

cd ../Auth.Infrastructure
dotnet add package MongoDB.Driver
dotnet add package BCrypt.Net-Next

# Add project references
cd ../Auth.API
dotnet add reference ../Auth.Application/Auth.Application.csproj
dotnet add reference ../Auth.Infrastructure/Auth.Infrastructure.csproj
```

**Features cần implement:**
- Login/Logout
- JWT token generation
- User CRUD
- Role management

### Phase 5: Parking Service

```bash
# Tạo projects (tương tự Auth Service)
# Implement features:
- Buildings CRUD
- Vehicle Types CRUD
- Floors & Zones CRUD
- Parking Slots CRUD
- Check-in/Check-out
- Parking Sessions
- Capacity management
- Shifts management
```

### Phase 6: Payment Service

```bash
# Tạo projects (tương tự Auth Service)
# Implement features:
- Fee Policies CRUD
- Fee calculation
- Payment processing
- Monthly Subscriptions CRUD
- Payment confirmation
```

### Phase 7: Report Service

```bash
# Tạo projects (tương tự Auth Service)
# Implement features:
- Revenue reports
- Vehicle flow analytics
- Occupancy statistics
- Shift reconciliation reports
- Dashboard aggregation
```

### Phase 8: Inter-service Communication

**Option 1: HTTP REST (đơn giản - dùng cho MVP)**

```csharp
// Parking Service cần gọi Auth Service để verify user
public class AuthServiceClient
{
    private readonly HttpClient _httpClient;
    
    public AuthServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    public async Task<UserDto> GetUserByIdAsync(string userId)
    {
        var response = await _httpClient.GetAsync($"/api/v1/users/{userId}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserDto>();
    }
}

// Register in Program.cs
builder.Services.AddHttpClient<AuthServiceClient>(client =>
{
    client.BaseAddress = new Uri("http://localhost:5001");
});
```

**Option 2: Message Queue (advanced - RabbitMQ)**

```bash
# Add RabbitMQ package
dotnet add package RabbitMQ.Client
dotnet add package MassTransit
dotnet add package MassTransit.RabbitMQ

# Lưu ý: docker-compose.yml hiện CHƯA có RabbitMQ.
# Nếu dùng message queue, tự thêm service rabbitmq vào docker-compose,
# hoặc chạy: docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```

### Phase 9: Testing

```bash
# Unit Tests
dotnet new xunit -n Auth.UnitTests -o tests/Auth.UnitTests
dotnet add package Moq
dotnet add package FluentAssertions

# Integration Tests
dotnet new xunit -n Integration.Tests -o tests/Integration.Tests
dotnet add package Microsoft.AspNetCore.Mvc.Testing
```

### Phase 10: Frontend Integration

```bash
# React app sẽ chỉ gọi Gateway
const API_BASE_URL = "http://localhost:5000/api/v1";

// Login
POST http://localhost:5000/api/v1/auth/login

// Check-in (routing tới Parking Service)
POST http://localhost:5000/api/v1/parking/parking-sessions/check-in

// Get reports (routing tới Report Service)
GET http://localhost:5000/api/v1/reports/revenue
```

## 🔧 Configuration

### Ocelot Routing Rules

File: `src/ApiGateway/ocelot.json`

```json
{
  "Routes": [
    {
      "DownstreamPathTemplate": "/api/v1/auth/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        { "Host": "localhost", "Port": 5001 }
      ],
      "UpstreamPathTemplate": "/api/v1/auth/{everything}",
      "UpstreamHttpMethod": [ "GET", "POST", "PUT", "DELETE" ]
    }
  ]
}
```

### MongoDB Connection Strings

```json
{
  "MongoDbSettings": {
    "ConnectionString": "mongodb+srv://<username>:<password>@cluster0.xxxxx.mongodb.net/?retryWrites=true&w=majority",
    "DatabaseName": "parking_auth_db"
  }
}
```

### JWT Settings

```json
{
  "JwtSettings": {
    "Secret": "your-super-secret-key-min-32-characters",
    "Issuer": "ParkingSystem",
    "Audience": "ParkingSystemUsers",
    "ExpirationMinutes": 60
  }
}
```

## 🧪 Testing

### Test Login Flow

```bash
# 1. Login qua Gateway
curl -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@parking.com",
    "password": "Admin@123"
  }'

# Response:
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": "...",
    "email": "admin@parking.com",
    "role": "Admin"
  }
}

# 2. Dùng token để gọi protected endpoints
curl -X GET http://localhost:5000/api/v1/parking/buildings \
  -H "Authorization: Bearer <token>"
```

## 📊 Monitoring & Debugging

### View logs

```bash
# Docker Compose logs
docker-compose logs -f gateway
docker-compose logs -f auth-service
docker-compose logs -f parking-service

# Hoặc xem tất cả
docker-compose logs -f
```

### Debug từng service

```bash
# Attach debugger trong Visual Studio/Rider
# Hoặc dùng dotnet watch
cd src/Services/Auth/Auth.API
dotnet watch run
```

## 🚨 Troubleshooting

### Gateway không routing được

```bash
# Check Ocelot config
cat src/ApiGateway/ocelot.json

# Check service health
curl http://localhost:5001/health
curl http://localhost:5002/health
```

### MongoDB connection failed

```bash
# Check MongoDB container
docker ps | grep mongo

# Test connection
mongo mongodb://admin:password123@localhost:27017
```

### Service không start

```bash
# Check port conflicts
netstat -ano | findstr :5000
netstat -ano | findstr :5001

# Kill process if needed
taskkill /PID <pid> /F
```

## 📚 Tài liệu tham khảo

- [Ocelot Documentation](https://ocelot.readthedocs.io/)
- [Microservices Architecture](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/)
- [MongoDB with .NET](https://www.mongodb.com/docs/drivers/csharp/)
- [JWT Authentication](https://jwt.io/)

## 🎯 Next Steps

1. ✅ Setup infrastructure (HOÀN THÀNH)
2. ⏳ Implement Shared libraries
3. ⏳ Implement Auth Service
4. ⏳ Implement Parking Service
5. ⏳ Implement Payment Service
6. ⏳ Implement Report Service
7. ⏳ Setup inter-service communication
8. ⏳ Integration testing
9. ⏳ Frontend integration

## 📞 Support

Nếu gặp vấn đề, check:
1. `docs/MICROSERVICES_ARCHITECTURE.md` - Chi tiết kiến trúc
2. `docs/PROJECT_DOCUMENTATION.md` - Business requirements
3. GitHub Issues

---

**Author**: Parking Manager Team  
**Last Updated**: 2026-06-16
