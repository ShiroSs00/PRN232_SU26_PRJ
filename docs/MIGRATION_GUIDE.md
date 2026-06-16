# Migration Guide: Monolith to Microservices

## 📋 Tổng quan

Hướng dẫn này giúp migrate từ cấu trúc **Monolithic** hiện tại sang **Microservices Architecture**.

## 🗂️ Cấu trúc hiện tại (Monolith)

```
PRN232_SU26_PRJ/
├── ParkingSystem.API/              # Single API
├── ParkingSystem.Application/      # Business logic
├── ParkingSystem.Domain/           # Domain entities
└── ParkingSystem.Infrastructure/   # Database & external services
```

## 🎯 Cấu trúc mới (Microservices)

```
PRN232_SU26_PRJ/
├── src/
│   ├── ApiGateway/                 # Ocelot Gateway
│   ├── Services/
│   │   ├── Auth/                   # Auth Service
│   │   ├── Parking/                # Parking Service
│   │   ├── Payment/                # Payment Service
│   │   └── Report/                 # Report Service
│   └── Shared/                     # Shared libraries
```

## 🔄 Migration Strategy

### Phase 1: Preparation (1 ngày)

#### 1.1. Backup code hiện tại

```bash
# Tạo branch backup
git checkout main
git checkout -b backup/monolith-before-migration
git push origin backup/monolith-before-migration

# Tạo branch migration
git checkout main
git checkout -b feature/microservices-architecture
```

#### 1.2. Phân tích dependencies

**Controllers cần di chuyển:**

| Controller | Service mới | MongoDB Collection |
|------------|-------------|-------------------|
| AuthController | Auth Service | users, roles |
| BuildingController | Parking Service | buildings |
| VehicleTypeController | Parking Service | vehicle_types |
| FloorController | Parking Service | floors |
| ZoneController | Parking Service | zones |
| ParkingSlotController | Parking Service | parking_slots |
| ParkingSessionController | Parking Service | parking_sessions |
| ShiftController | Parking Service | shifts |
| FeePolicyController | Payment Service | fee_policies |
| PaymentController | Payment Service | payments |
| SubscriptionController | Payment Service | subscriptions |
| ReportController | Report Service | (read from all collections) |

#### 1.3. Identify shared code

**Code cần move sang Shared:**
- `BaseEntity` → `ParkingSystem.Common`
- DTOs → `ParkingSystem.Contracts`
- MongoDB configuration → Mỗi service tự quản lý
- JWT utilities → `ParkingSystem.Common`

---

### Phase 2: Setup Infrastructure (1-2 ngày)

#### 2.1. Tạo Shared Libraries

```bash
# Common library
dotnet new classlib -n ParkingSystem.Common -o src/Shared/ParkingSystem.Common
dotnet add src/Shared/ParkingSystem.Common package Microsoft.Extensions.DependencyInjection.Abstractions

# Contracts library
dotnet new classlib -n ParkingSystem.Contracts -o src/Shared/ParkingSystem.Contracts
```

**Di chuyển code:**

`ParkingSystem.Common/` structure:
```
Common/
├── Entities/
│   └── BaseEntity.cs           # From ParkingSystem.Domain
├── Exceptions/
│   └── BusinessException.cs
├── Extensions/
│   └── ServiceCollectionExtensions.cs
├── Middleware/
│   └── ExceptionHandlingMiddleware.cs
└── Results/
    └── Result.cs
```

`ParkingSystem.Contracts/` structure:
```
Contracts/
├── DTOs/
│   ├── Auth/
│   │   ├── LoginRequest.cs
│   │   ├── LoginResponse.cs
│   │   └── UserDto.cs
│   ├── Parking/
│   │   ├── CheckInRequest.cs
│   │   └── ParkingSessionDto.cs
│   └── Payment/
│       └── PaymentDto.cs
├── Events/
│   ├── VehicleCheckedInEvent.cs
│   └── VehicleCheckedOutEvent.cs
└── Interfaces/
    └── IAuthServiceClient.cs
```

#### 2.2. Setup API Gateway

```bash
# Tạo Gateway project
dotnet new webapi -n ParkingSystem.Gateway -o src/ApiGateway
cd src/ApiGateway

# Add Ocelot
dotnet add package Ocelot
dotnet add package Ocelot.Cache.CacheManager

# Copy ocelot.json đã tạo
cp ../../ocelot.json .
```

---

### Phase 3: Migrate Auth Service (2 ngày)

#### 3.1. Tạo Auth Service structure

```bash
dotnet new webapi -n Auth.API -o src/Services/Auth/Auth.API
dotnet new classlib -n Auth.Application -o src/Services/Auth/Auth.Application
dotnet new classlib -n Auth.Domain -o src/Services/Auth/Auth.Domain
dotnet new classlib -n Auth.Infrastructure -o src/Services/Auth/Auth.Infrastructure
```

#### 3.2. Di chuyển entities

**From:** `ParkingSystem.Domain/`
**To:** `src/Services/Auth/Auth.Domain/Entities/`

```
Auth.Domain/Entities/
├── User.cs
├── Role.cs
└── RefreshToken.cs
```

#### 3.3. Di chuyển MongoDB collections

**From:** `ParkingSystem.Infrastructure/Data/`
**To:** `src/Services/Auth/Auth.Infrastructure/Data/`

```csharp
// Auth.Infrastructure/Data/AuthDbContext.cs
public class AuthDbContext
{
    private readonly IMongoDatabase _database;
    
    public IMongoCollection<User> Users { get; }
    public IMongoCollection<Role> Roles { get; }
    
    public AuthDbContext(IOptions<DatabaseSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        _database = client.GetDatabase(settings.Value.DatabaseName);
        
        Users = _database.GetCollection<User>("users");
        Roles = _database.GetCollection<Role>("roles");
    }
}
```

#### 3.4. Di chuyển Controllers

**From:** `ParkingSystem.API/Controllers/AuthController.cs`
**To:** `src/Services/Auth/Auth.API/Controllers/AuthController.cs`

```csharp
// Auth.API/Controllers/AuthController.cs
[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        return Ok(result);
    }
    
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        // Implementation
        return Ok();
    }
    
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        // Implementation
        return Ok();
    }
}
```

#### 3.5. Configure Auth.API

```csharp
// Auth.API/Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// MongoDB
builder.Services.Configure<DatabaseSettings>(
    builder.Configuration.GetSection("DatabaseSettings"));
builder.Services.AddSingleton<AuthDbContext>();

// Application services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Secret"]))
        };
    });

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

#### 3.6. Test Auth Service

```bash
# Run Auth Service
cd src/Services/Auth/Auth.API
dotnet run --urls="http://localhost:5001"

# Test login
curl -X POST http://localhost:5001/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@parking.com",
    "password": "Admin@123"
  }'
```

---

### Phase 4: Migrate Parking Service (3-4 ngày)

#### 4.1. Tạo structure

```bash
dotnet new webapi -n Parking.API -o src/Services/Parking/Parking.API
dotnet new classlib -n Parking.Application -o src/Services/Parking/Parking.Application
dotnet new classlib -n Parking.Domain -o src/Services/Parking/Parking.Domain
dotnet new classlib -n Parking.Infrastructure -o src/Services/Parking/Parking.Infrastructure
```

#### 4.2. Di chuyển entities

**From:** `ParkingSystem.Domain/`
**To:** `src/Services/Parking/Parking.Domain/Entities/`

```
Parking.Domain/Entities/
├── Building.cs
├── Floor.cs
├── Zone.cs
├── VehicleType.cs
├── ParkingSlot.cs
├── ParkingSession.cs
└── Shift.cs
```

#### 4.3. Di chuyển Controllers

**Controllers cần move:**
- `BuildingController` → `Parking.API/Controllers/BuildingController.cs`
- `VehicleTypeController` → `Parking.API/Controllers/VehicleTypeController.cs`
- `FloorController` → `Parking.API/Controllers/FloorController.cs`
- `ZoneController` → `Parking.API/Controllers/ZoneController.cs`
- `ParkingSlotController` → `Parking.API/Controllers/ParkingSlotController.cs`
- `ParkingSessionController` → `Parking.API/Controllers/ParkingSessionController.cs`
- `ShiftController` → `Parking.API/Controllers/ShiftController.cs`

#### 4.4. Update routes

**Trước (Monolith):**
```
GET /api/v1/buildings
```

**Sau (Microservices qua Gateway):**
```
Gateway: GET /api/v1/parking/buildings
  ↓ routing
Parking Service: GET /api/v1/buildings
```

#### 4.5. Add inter-service call

Parking Service cần gọi Auth Service để verify user:

```csharp
// Parking.Infrastructure/Clients/AuthServiceClient.cs
public class AuthServiceClient : IAuthServiceClient
{
    private readonly HttpClient _httpClient;
    
    public AuthServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    public async Task<UserDto> GetUserByIdAsync(string userId)
    {
        var response = await _httpClient.GetAsync($"/api/v1/auth/users/{userId}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserDto>();
    }
}

// Parking.API/Program.cs
builder.Services.AddHttpClient<IAuthServiceClient, AuthServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:AuthService"]);
});
```

---

### Phase 5: Migrate Payment Service (2-3 ngày)

#### 5.1. Tạo structure

```bash
dotnet new webapi -n Payment.API -o src/Services/Payment/Payment.API
dotnet new classlib -n Payment.Application -o src/Services/Payment/Payment.Application
dotnet new classlib -n Payment.Domain -o src/Services/Payment/Payment.Domain
dotnet new classlib -n Payment.Infrastructure -o src/Services/Payment/Payment.Infrastructure
```

#### 5.2. Di chuyển entities

```
Payment.Domain/Entities/
├── FeePolicy.cs
├── Payment.cs
└── Subscription.cs
```

#### 5.3. Di chuyển Controllers

- `FeePolicyController`
- `PaymentController`
- `SubscriptionController`

#### 5.4. Add event-driven communication

Khi Parking Service check-out xe, publish event để Payment Service tạo payment:

```csharp
// In Parking Service
public async Task CheckOutAsync(string sessionId)
{
    var session = await _sessionRepository.GetByIdAsync(sessionId);
    session.CheckOut();
    await _sessionRepository.UpdateAsync(session);
    
    // Publish event
    await _eventPublisher.PublishAsync(new VehicleCheckedOutEvent
    {
        SessionId = session.Id,
        PlateNumber = session.PlateNumber,
        TotalFee = session.TotalFee,
        CheckOutTime = session.CheckOutTime
    });
}

// In Payment Service - Event Handler
public class VehicleCheckedOutEventHandler : IEventHandler<VehicleCheckedOutEvent>
{
    public async Task HandleAsync(VehicleCheckedOutEvent @event)
    {
        // Tự động tạo payment
        var payment = new Payment
        {
            ParkingSessionId = @event.SessionId,
            Amount = @event.TotalFee,
            Status = PaymentStatus.Pending
        };
        await _paymentRepository.CreateAsync(payment);
    }
}
```

---

### Phase 6: Migrate Report Service (1-2 ngày)

#### 6.1. Tạo structure

```bash
dotnet new webapi -n Report.API -o src/Services/Report/Report.API
dotnet new classlib -n Report.Application -o src/Services/Report/Report.Application
dotnet new classlib -n Report.Domain -o src/Services/Report/Report.Domain
dotnet new classlib -n Report.Infrastructure -o src/Services/Report/Report.Infrastructure
```

#### 6.2. Report Service đặc biệt

Report Service cần **READ** data từ nhiều databases:

**Option 1: Direct database access (đơn giản)**
```csharp
// Report.Infrastructure/Data/ReportDbContext.cs
public class ReportDbContext
{
    public IMongoCollection<ParkingSession> ParkingSessions { get; }
    public IMongoCollection<Payment> Payments { get; }
    public IMongoCollection<Building> Buildings { get; }
    
    public ReportDbContext(IOptions<DatabaseSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var database = client.GetDatabase(settings.Value.DatabaseName);
        
        // Read-only access
        ParkingSessions = database.GetCollection<ParkingSession>("parking_sessions");
        Payments = database.GetCollection<Payment>("payments");
        Buildings = database.GetCollection<Building>("buildings");
    }
}
```

**Option 2: HTTP calls to other services (chuẩn microservices)**
```csharp
public async Task<RevenueReportDto> GetRevenueReportAsync(DateTime from, DateTime to)
{
    // Gọi Payment Service
    var payments = await _paymentServiceClient.GetPaymentsAsync(from, to);
    
    // Gọi Parking Service
    var sessions = await _parkingServiceClient.GetSessionsAsync(from, to);
    
    // Aggregate data
    return new RevenueReportDto
    {
        TotalRevenue = payments.Sum(p => p.Amount),
        TotalSessions = sessions.Count
    };
}
```

---

### Phase 7: Update Frontend (1 ngày)

#### 7.1. Update API base URL

**Trước (Monolith):**
```typescript
// src/config/api.ts
export const API_BASE_URL = "http://localhost:5000/api/v1";
```

**Sau (Microservices qua Gateway):**
```typescript
// src/config/api.ts
export const API_BASE_URL = "http://localhost:5000/api/v1"; // Không đổi!

// Gateway sẽ routing:
// /api/v1/auth/* → Auth Service
// /api/v1/parking/* → Parking Service
// /api/v1/payments/* → Payment Service
// /api/v1/reports/* → Report Service
```

#### 7.2. Update API calls

**Chỉ cần thêm prefix cho non-auth endpoints:**

```typescript
// Before
GET /api/v1/buildings

// After
GET /api/v1/parking/buildings

// Auth endpoints giữ nguyên
POST /api/v1/auth/login
```

---

### Phase 8: Testing & Validation (2-3 ngày)

#### 8.1. Unit Testing

```bash
# Test mỗi service độc lập
dotnet test src/Services/Auth/Auth.UnitTests
dotnet test src/Services/Parking/Parking.UnitTests
dotnet test src/Services/Payment/Payment.UnitTests
```

#### 8.2. Integration Testing

```bash
# Test full flow qua Gateway
1. Login qua Gateway
2. Get buildings qua Gateway
3. Check-in vehicle qua Gateway
4. Check-out qua Gateway
5. Get report qua Gateway
```

#### 8.3. Load Testing

```bash
# Dùng k6 hoặc Apache Bench
k6 run load-test.js
```

---

## 🔄 Rollback Plan

Nếu migration thất bại:

```bash
# Quay lại monolith
git checkout main

# Hoặc restore từ backup
git checkout backup/monolith-before-migration
```

---

## ✅ Migration Checklist

### Infrastructure
- [ ] Tạo Shared libraries (Common + Contracts)
- [ ] Setup Ocelot Gateway
- [ ] Setup Docker Compose
- [ ] Setup MongoDB với collections riêng

### Services
- [ ] Auth Service hoạt động độc lập
- [ ] Parking Service hoạt động độc lập
- [ ] Payment Service hoạt động độc lập
- [ ] Report Service hoạt động độc lập

### Inter-service Communication
- [ ] Parking Service gọi được Auth Service
- [ ] Payment Service subscribe event từ Parking Service
- [ ] Report Service đọc được data từ tất cả services

### Gateway
- [ ] Ocelot routing hoạt động
- [ ] JWT authentication qua Gateway
- [ ] Rate limiting configured
- [ ] CORS configured

### Frontend
- [ ] Update API endpoints
- [ ] Login flow hoạt động qua Gateway
- [ ] Check-in/out flow hoạt động
- [ ] Reports hoạt động

### Testing
- [ ] Unit tests pass cho tất cả services
- [ ] Integration tests pass
- [ ] Load test acceptable
- [ ] Manual QA pass

### Documentation
- [ ] API documentation (Swagger)
- [ ] Deployment guide
- [ ] Troubleshooting guide

---

## 📊 Timeline Summary

| Phase | Duration | Tasks |
|-------|----------|-------|
| Preparation | 1 ngày | Backup, analysis, planning |
| Infrastructure | 1-2 ngày | Shared libs, Gateway, Docker |
| Auth Service | 2 ngày | Entities, repos, controllers, testing |
| Parking Service | 3-4 ngày | Entities, repos, controllers, testing |
| Payment Service | 2-3 ngày | Entities, repos, controllers, events |
| Report Service | 1-2 ngày | Aggregation logic, testing |
| Frontend Update | 1 ngày | Update endpoints, testing |
| Testing & QA | 2-3 ngày | Integration tests, load tests |
| **TOTAL** | **13-18 ngày** | Full migration |

---

## 🚨 Common Issues

### Issue 1: Port conflicts
```bash
# Solution: Check ports trước khi run
netstat -ano | findstr :5000
```

### Issue 2: MongoDB connection string different per service
```json
// Solution: Mỗi service có config riêng
{
  "DatabaseSettings": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "ParkingSystemDB"
  }
}
```

### Issue 3: JWT validation failed cross-service
```csharp
// Solution: Tất cả services dùng cùng JWT secret
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Secret"]))
        };
    });
```

---

**Next Step:** Bắt đầu từ Phase 2 (Setup Infrastructure) trong MICROSERVICES_SETUP.md
