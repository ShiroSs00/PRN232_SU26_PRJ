# Parking Manager - Microservices Architecture

## Tổng quan kiến trúc

```
┌─────────────────────────────────────────────────────────┐
│              Client (React App - Port 5173)              │
└─────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────┐
│          Ocelot API Gateway (Port 5000)                  │
│  - Authentication middleware                             │
│  - Rate limiting                                         │
│  - Request routing                                       │
│  - Load balancing                                        │
│  - CORS configuration                                    │
└─────────────────────────────────────────────────────────┘
            │              │              │              │
    ┌───────┴──────┐  ┌────┴────┐  ┌────┴────┐  ┌─────┴─────┐
    ▼              ▼  ▼         ▼  ▼         ▼  ▼           ▼
┌────────┐   ┌─────────┐   ┌─────────┐   ┌─────────┐
│  Auth   │   │ Parking │   │ Payment │   │ Report  │
│ Service │   │ Service │   │ Service │   │ Service │
│ :5001  │   │  :5002  │   │  :5003  │   │  :5004  │
└────────┘   └─────────┘   └─────────┘   └─────────┘
    │              │              │              │
    └──────────────┴──────────────┴──────────────┘
                           │
                           ▼
            ┌──────────────────────────┐
            │   MongoDB Database       │
            │   - auth_db              │
            │   - parking_db           │
            │   - payment_db           │
            │   - report_db            │
            └──────────────────────────┘
```

## Services Chi tiết

### 1. Ocelot API Gateway (Port 5000)

**Vai trò:**
- Single entry point cho tất cả client requests
- Routing requests đến services phù hợp
- Authentication & Authorization
- Rate limiting & throttling
- Load balancing
- CORS configuration

**Dependencies:**
- Ocelot
- Ocelot.Provider.Consul (service discovery - optional)
- Microsoft.AspNetCore.Authentication.JwtBearer

**Configuration:**
- `ocelot.json`: Route configuration
- `ocelot.Development.json`: Development overrides
- `ocelot.Production.json`: Production overrides

---

### 2. Auth Service (Port 5001)

**Trách nhiệm:**
- User authentication (login/logout)
- JWT token generation & validation
- Refresh token management
- User management
- Role & permission management

**Endpoints:**
```
POST   /api/v1/auth/login
POST   /api/v1/auth/logout
POST   /api/v1/auth/refresh-token
GET    /api/v1/auth/me
GET    /api/v1/users
GET    /api/v1/users/{id}
POST   /api/v1/users
PUT    /api/v1/users/{id}
DELETE /api/v1/users/{id}
GET    /api/v1/roles
POST   /api/v1/roles
```

**Database Collections (auth_db):**
- users
- roles
- user_roles
- refresh_tokens

**Dependencies:**
- MongoDB.Driver
- BCrypt.Net (password hashing)
- System.IdentityModel.Tokens.Jwt

---

### 3. Parking Service (Port 5002)

**Trách nhiệm:**
- Building, floor, zone management
- Vehicle type management
- Parking slot management
- Parking session (check-in/check-out)
- Shift management
- Capacity checking

**Endpoints:**
```
# Buildings
GET    /api/v1/buildings
GET    /api/v1/buildings/{id}
POST   /api/v1/buildings
PUT    /api/v1/buildings/{id}
DELETE /api/v1/buildings/{id}

# Vehicle Types
GET    /api/v1/vehicle-types
GET    /api/v1/vehicle-types/{id}
POST   /api/v1/vehicle-types
PUT    /api/v1/vehicle-types/{id}
DELETE /api/v1/vehicle-types/{id}

# Floors
GET    /api/v1/floors
POST   /api/v1/floors
PUT    /api/v1/floors/{id}
DELETE /api/v1/floors/{id}

# Zones
GET    /api/v1/zones
GET    /api/v1/zones/by-building/{buildingId}
POST   /api/v1/zones
PUT    /api/v1/zones/{id}
DELETE /api/v1/zones/{id}

# Parking Slots
GET    /api/v1/parking-slots
GET    /api/v1/parking-slots/{id}
GET    /api/v1/parking-slots/available
POST   /api/v1/parking-slots
PUT    /api/v1/parking-slots/{id}
PATCH  /api/v1/parking-slots/{id}/status
DELETE /api/v1/parking-slots/{id}

# Parking Sessions
GET    /api/v1/parking-sessions
GET    /api/v1/parking-sessions/{id}
GET    /api/v1/parking-sessions/active
GET    /api/v1/parking-sessions/active/by-plate/{plateNumber}
POST   /api/v1/parking-sessions/check-in
POST   /api/v1/parking-sessions/{id}/check-out
POST   /api/v1/parking-sessions/{id}/cancel
POST   /api/v1/parking-sessions/{id}/mark-lost-ticket

# Shifts
GET    /api/v1/shifts
GET    /api/v1/shifts/{id}
GET    /api/v1/shifts/current
POST   /api/v1/shifts/open
POST   /api/v1/shifts/{id}/close
```

**Database Collections (parking_db):**
- buildings
- floors
- zones
- vehicle_types
- parking_slots
- parking_sessions
- shifts

**Inter-service Communication:**
- Gọi Payment Service để calculate fee khi check-out
- Gọi Payment Service để verify subscription status
- Gọi Auth Service để validate user permissions

---

### 4. Payment Service (Port 5003)

**Trách nhiệm:**
- Fee policy management
- Fee calculation
- Payment processing
- Monthly subscription management
- Payment reconciliation

**Endpoints:**
```
# Fee Policies
GET    /api/v1/fee-policies
GET    /api/v1/fee-policies/{id}
GET    /api/v1/fee-policies/active
POST   /api/v1/fee-policies
PUT    /api/v1/fee-policies/{id}
DELETE /api/v1/fee-policies/{id}
POST   /api/v1/fee-policies/calculate

# Payments
GET    /api/v1/payments
GET    /api/v1/payments/{id}
GET    /api/v1/payments/by-session/{sessionId}
POST   /api/v1/payments
POST   /api/v1/payments/{id}/confirm
POST   /api/v1/payments/{id}/cancel
POST   /api/v1/payments/{id}/refund

# Subscriptions
GET    /api/v1/subscriptions
GET    /api/v1/subscriptions/{id}
GET    /api/v1/subscriptions/active
GET    /api/v1/subscriptions/expiring
GET    /api/v1/subscriptions/by-plate/{plateNumber}
POST   /api/v1/subscriptions
PUT    /api/v1/subscriptions/{id}
POST   /api/v1/subscriptions/{id}/renew
POST   /api/v1/subscriptions/{id}/suspend
POST   /api/v1/subscriptions/{id}/cancel
```

**Database Collections (payment_db):**
- fee_policies
- payments
- subscriptions

**Inter-service Communication:**
- Gọi Parking Service để get parking session details
- Gọi Parking Service để get vehicle type info

---

### 5. Report Service (Port 5004)

**Trách nhiệm:**
- Revenue reports
- Vehicle flow analytics
- Occupancy statistics
- Shift reconciliation reports
- Dashboard aggregation

**Endpoints:**
```
GET /api/v1/reports/dashboard
GET /api/v1/reports/revenue
GET /api/v1/reports/vehicle-flow
GET /api/v1/reports/occupancy
GET /api/v1/reports/peak-hours
GET /api/v1/reports/shift-reconciliation
GET /api/v1/reports/subscriptions
GET /api/v1/reports/export/revenue (Excel/PDF)
```

**Database Collections (report_db):**
- Chủ yếu READ từ các services khác
- Có thể cache aggregated data

**Inter-service Communication:**
- Read data từ tất cả services để tạo reports

---

## Solution Structure

```
PRN232_SU26_PRJ/
│
├── src/
│   ├── ApiGateway/
│   │   ├── Program.cs
│   │   ├── ocelot.json
│   │   ├── ocelot.Development.json
│   │   └── appsettings.json
│   │
│   ├── Services/
│   │   ├── Auth/
│   │   │   ├── Auth.API/
│   │   │   ├── Auth.Application/
│   │   │   ├── Auth.Domain/
│   │   │   └── Auth.Infrastructure/
│   │   │
│   │   ├── Parking/
│   │   │   ├── Parking.API/
│   │   │   ├── Parking.Application/
│   │   │   ├── Parking.Domain/
│   │   │   └── Parking.Infrastructure/
│   │   │
│   │   ├── Payment/
│   │   │   ├── Payment.API/
│   │   │   ├── Payment.Application/
│   │   │   ├── Payment.Domain/
│   │   │   └── Payment.Infrastructure/
│   │   │
│   │   └── Report/
│   │       ├── Report.API/
│   │       ├── Report.Application/
│   │       ├── Report.Domain/
│   │       └── Report.Infrastructure/
│   │
│   └── Shared/
│       ├── Shared.Common/        # Common utilities
│       ├── Shared.Contracts/     # DTOs, Events
│       └── Shared.Infrastructure/ # Base classes
│
├── docker-compose.yml
├── docker-compose.override.yml
└── PRN232_PRJ.sln
```

## Database Strategy

### Option 1: Database per Service (Recommended)
Mỗi service có MongoDB database riêng:
- `auth_db`
- `parking_db`
- `payment_db`
- `report_db`

**Ưu điểm:**
- True microservices isolation
- Schema changes không affect services khác
- Scale independently

**Nhược điểm:**
- Không thể JOIN across databases
- Phải implement eventual consistency

### Option 2: Shared Database (Easier)
1 MongoDB database, mỗi service có collections riêng:
- Prefix: `auth_*`, `parking_*`, `payment_*`, `report_*`

**Ưu điểm:**
- Dễ setup
- Có thể query across collections
- Easier for development

**Nhược điểm:**
- Không phải true microservices
- Tight coupling

**Quyết định: Dùng Option 2 cho môn học (đơn giản hơn)**

---

## Inter-service Communication

### Synchronous: HTTP REST (Primary)

**Auth Service Client (dùng trong các services khác):**
```csharp
public interface IAuthServiceClient
{
    Task<UserDto> GetUserByIdAsync(string userId);
    Task<bool> ValidateTokenAsync(string token);
}
```

**Payment Service Client (dùng trong Parking Service):**
```csharp
public interface IPaymentServiceClient
{
    Task<decimal> CalculateFeeAsync(CalculateFeeRequest request);
    Task<SubscriptionDto> GetActiveSubscriptionByPlateAsync(string plateNumber);
}
```

**Parking Service Client (dùng trong Report Service):**
```csharp
public interface IParkingServiceClient
{
    Task<IEnumerable<ParkingSessionDto>> GetSessionsAsync(DateTime from, DateTime to);
    Task<OccupancyDto> GetOccupancyAsync();
}
```

### Asynchronous: Message Queue (Optional - Phase 2)

Sử dụng RabbitMQ hoặc Kafka cho events:
- `VehicleCheckedInEvent`
- `VehicleCheckedOutEvent`
- `PaymentCompletedEvent`
- `SubscriptionExpiredEvent`

---

## Authentication & Authorization Flow

```
1. User → Gateway → Auth Service
   POST /api/v1/auth/login
   {email, password}
   
2. Auth Service validate và generate JWT
   Response: {accessToken, refreshToken, user}

3. Client lưu token và gửi trong header:
   Authorization: Bearer {accessToken}

4. Gateway validate JWT từ Auth Service
   - Nếu valid: forward request đến service
   - Nếu invalid: return 401 Unauthorized

5. Service nhận request đã authenticated
   - UserId từ JWT claims
   - Roles từ JWT claims
```

---

## Configuration

### Environment Variables

**Gateway:**
```env
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://localhost:5000
AUTH_SERVICE_URL=http://localhost:5001
PARKING_SERVICE_URL=http://localhost:5002
PAYMENT_SERVICE_URL=http://localhost:5003
REPORT_SERVICE_URL=http://localhost:5004
JWT_SECRET=your-secret-key-here
```

**Auth Service:**
```env
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://localhost:5001
MONGODB_CONNECTION_STRING=mongodb+srv://<username>:<password>@cluster0.xxxxx.mongodb.net/?retryWrites=true&w=majority
MONGODB_DATABASE_NAME=parking_auth_db
JWT_SECRET=your-secret-key-here
JWT_ISSUER=ParkingSystemAPI
JWT_AUDIENCE=ParkingSystemClient
JWT_EXPIRY_MINUTES=60
```

**Parking Service:**
```env
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://localhost:5002
MONGODB_CONNECTION_STRING=mongodb+srv://<username>:<password>@cluster0.xxxxx.mongodb.net/?retryWrites=true&w=majority
MONGODB_DATABASE_NAME=parking_main_db
AUTH_SERVICE_URL=http://localhost:5001
PAYMENT_SERVICE_URL=http://localhost:5003
```

**Payment Service:**
```env
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://localhost:5003
MONGODB_CONNECTION_STRING=mongodb+srv://<username>:<password>@cluster0.xxxxx.mongodb.net/?retryWrites=true&w=majority
MONGODB_DATABASE_NAME=parking_payment_db
PARKING_SERVICE_URL=http://localhost:5002
```

**Report Service:**
```env
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://localhost:5004
MONGODB_CONNECTION_STRING=mongodb+srv://<username>:<password>@cluster0.xxxxx.mongodb.net/?retryWrites=true&w=majority
MONGODB_DATABASE_NAME=parking_report_db
AUTH_SERVICE_URL=http://localhost:5001
PARKING_SERVICE_URL=http://localhost:5002
PAYMENT_SERVICE_URL=http://localhost:5003
```

---

## Development Workflow

### 1. Local Development (Without Docker)

Chạy từng service manually:
```bash
# Terminal 1: Gateway
cd src/ApiGateway
dotnet run

# Terminal 2: Auth Service
cd src/Services/Auth/Auth.API
dotnet run

# Terminal 3: Parking Service
cd src/Services/Parking/Parking.API
dotnet run

# Terminal 4: Payment Service
cd src/Services/Payment/Payment.API
dotnet run

# Terminal 5: Report Service
cd src/Services/Report/Report.API
dotnet run
```

### 2. Local Development (With Docker Compose)

```bash
docker-compose up --build
```

---

## Testing Strategy

### Unit Tests
- Mỗi service có project test riêng
- Test business logic trong Application layer
- Test domain models

### Integration Tests
- Test API endpoints
- Test database operations
- Test inter-service communication

### End-to-End Tests
- Test complete flows qua Gateway
- Test authentication flow
- Test check-in/check-out flow
- Test payment flow

---

## Deployment

### Development
- Local machines
- Docker Compose

### Staging/Production Options

**Option 1: Azure**
- Azure Container Apps
- Azure Cosmos DB (MongoDB API)
- Azure API Management (thay Ocelot)

**Option 2: AWS**
- AWS ECS/EKS
- AWS DocumentDB (MongoDB compatible)
- AWS API Gateway

**Option 3: VPS**
- Docker Compose
- MongoDB Atlas
- Nginx reverse proxy

---

## Migration Plan từ Monolith

### Phase 1: Setup Infrastructure (HIỆN TẠI)
- [x] Tạo solution structure mới
- [ ] Setup Gateway project
- [ ] Setup 4 service projects
- [ ] Setup Shared projects
- [ ] Configure Docker Compose

### Phase 2: Auth Service
- [ ] Move authentication logic
- [ ] Implement JWT generation
- [ ] User management APIs
- [ ] Test through Gateway

### Phase 3: Core Services
- [ ] Parking Service implementation
- [ ] Payment Service implementation  
- [ ] Report Service implementation
- [ ] Inter-service communication

### Phase 4: Frontend Integration
- [ ] Update API client
- [ ] Update authentication flow
- [ ] Update all API calls

### Phase 5: Testing & Polish
- [ ] Integration testing
- [ ] Performance testing
- [ ] Documentation
- [ ] Demo preparation

---

## Best Practices

### 1. API Versioning
- Sử dụng URL versioning: `/api/v1/...`
- Dễ maintain multiple versions

### 2. Error Handling
- Consistent error response format
- Proper HTTP status codes
- Detailed error messages for development

### 3. Logging
- Structured logging (Serilog)
- Correlation ID để trace requests across services
- Log levels: Debug, Info, Warning, Error

### 4. Health Checks
- Implement `/health` endpoint cho mỗi service
- Gateway check health của tất cả services

### 5. Security
- HTTPS only in production
- JWT validation
- Input validation
- SQL injection prevention (dùng MongoDB nên ít risk hơn)
- XSS prevention

### 6. Performance
- Response caching
- Database indexing
- Async/await properly
- Connection pooling

---

## FAQ

### 1. Tại sao dùng MongoDB thay vì PostgreSQL?
- Code hiện tại đã setup MongoDB
- Document model phù hợp với microservices
- Flexible schema

### 2. Có cần Service Discovery (Consul/Eureka) không?
- Không bắt buộc cho môn học
- Hardcode URLs trong config đủ
- Có thể thêm sau nếu muốn

### 3. Có cần Message Queue không?
- Không bắt buộc cho MVP
- HTTP REST đủ cho synchronous operations
- Có thể thêm sau cho async events

### 4. Làm sao để debug?
- Run services locally (không dùng Docker)
- Attach debugger cho từng service
- Xem logs trong console

### 5. Deployment như thế nào?
- Development: Docker Compose local
- Production: Azure/AWS hoặc VPS với Docker

---

## Resources

- [Ocelot Documentation](https://ocelot.readthedocs.io/)
- [Microservices Patterns](https://microservices.io/patterns/)
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [MongoDB Best Practices](https://www.mongodb.com/docs/manual/administration/production-notes/)
