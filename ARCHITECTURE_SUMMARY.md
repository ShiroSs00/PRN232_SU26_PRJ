# Kiến Trúc Microservices cho Parking Manager System

## ✅ Đã Setup Xong

### 1. Tài liệu
- ✅ `docs/MICROSERVICES_ARCHITECTURE.md` - Kiến trúc tổng quan, phân chia services
- ✅ `docs/MIGRATION_GUIDE.md` - Hướng dẫn migrate từ monolith sang microservices
- ✅ `docs/DEPLOYMENT_GUIDE.md` - Hướng dẫn deploy production
- ✅ `MICROSERVICES_SETUP.md` - Hướng dẫn setup và chạy local

### 2. Configuration Files
- ✅ `src/ApiGateway/ocelot.json` - Ocelot routing configuration
- ✅ `src/ApiGateway/ocelot.Development.json` - Ocelot development config
- ✅ `docker-compose.yml` - Docker setup cho tất cả services
- ✅ `.env.example` - Template cho environment variables

### 3. Scripts
- ✅ `scripts/run-local.sh` - Script chạy tất cả services
- ✅ `scripts/stop-local.sh` - Script dừng tất cả services

---

## 🏗️ Kiến Trúc Tổng Quan

```
┌─────────────────────────────────┐
│      Client (React App)          │
│      http://localhost:5173       │
└─────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────┐
│    Ocelot API Gateway            │
│    http://localhost:5000         │
│    - JWT Authentication          │
│    - Rate Limiting               │
│    - Request Routing             │
│    - Load Balancing              │
└─────────────────────────────────┘
        │         │         │         │
   ┌────┘         │         │         └────┐
   ▼              ▼         ▼              ▼
┌──────┐   ┌──────────┐  ┌─────────┐  ┌──────────┐
│ Auth │   │ Parking  │  │ Payment │  │ Report   │
│:5001 │   │  :5002   │  │  :5003  │  │  :5004   │
└──────┘   └──────────┘  └─────────┘  └──────────┘
   │              │           │             │
   └──────┬───────┴───────┬───┴─────────────┘
          │               │
          ▼               ▼
   ┌────────────────────────────────┐
   │      MongoDB Atlas (Cloud)         │
   │  - parking_auth_db                 │
   │  - parking_main_db                 │
   │  - parking_payment_db              │
   │  - parking_report_db               │
   └────────────────────────────────┘
```

---

## 📦 Phân Chia Services

### 1. Auth Service (Port 5001)
**Database:** `parking_auth_db`

**Chức năng:**
- User authentication (login/logout)
- JWT token generation & validation
- User management (CRUD)
- Role & permission management

**API Endpoints:**
```
POST /api/v1/auth/login
POST /api/v1/auth/logout
GET  /api/v1/auth/me
POST /api/v1/auth/refresh-token
GET  /api/v1/users
POST /api/v1/users
PUT  /api/v1/users/{id}
DELETE /api/v1/users/{id}
GET  /api/v1/roles
POST /api/v1/roles
```

**Collections:**
- Users (Id, FullName, Email, PasswordHash, PhoneNumber, Roles[], IsActive, CreatedAt)
- Roles (Id, Name, Description, Permissions[])
- RefreshTokens (Id, UserId, Token, ExpiresAt, IsRevoked)

---

### 2. Parking Service (Port 5002)
**Database:** `parking_main_db`

**Chức năng:**
- Building, floor, zone management
- Vehicle type management
- Parking slot management
- Parking session (check-in/check-out)
- Shift management
- Capacity checking

**API Endpoints:**
```
# Buildings
GET/POST/PUT/DELETE /api/v1/buildings/*

# Vehicle Types
GET/POST/PUT/DELETE /api/v1/vehicle-types/*

# Floors & Zones
GET/POST/PUT/DELETE /api/v1/floors/*
GET/POST/PUT/DELETE /api/v1/zones/*

# Parking Slots
GET/POST/PUT/DELETE /api/v1/parking-slots/*
PATCH /api/v1/parking-slots/{id}/status

# Parking Sessions
GET  /api/v1/parking-sessions
POST /api/v1/parking-sessions/check-in
POST /api/v1/parking-sessions/{id}/check-out
GET  /api/v1/parking-sessions/active/by-plate/{plateNumber}

# Shifts
GET  /api/v1/shifts
POST /api/v1/shifts/open
POST /api/v1/shifts/{id}/close
```

**Collections:**
- Buildings
- Floors
- Zones
- VehicleTypes
- ParkingSlots
- ParkingSessions
- Shifts

---

### 3. Payment Service (Port 5003)
**Database:** `parking_payment_db`

**Chức năng:**
- Fee policy management
- Fee calculation
- Payment processing
- Monthly subscription management
- Payment reconciliation

**API Endpoints:**
```
# Fee Policies
GET/POST/PUT/DELETE /api/v1/fee-policies/*
POST /api/v1/fee-policies/calculate

# Payments
GET  /api/v1/payments
POST /api/v1/payments/{id}/confirm
POST /api/v1/payments/{id}/refund

# Subscriptions
GET/POST/PUT /api/v1/subscriptions/*
GET  /api/v1/subscriptions/active/by-plate/{plateNumber}
POST /api/v1/subscriptions/{id}/renew
```

**Collections:**
- FeePolicies
- Payments
- Subscriptions

---

### 4. Report Service (Port 5004)
**Database:** `parking_report_db` (hoặc read từ các DB khác)

**Chức năng:**
- Revenue reports
- Vehicle flow analytics
- Occupancy statistics
- Dashboard aggregation
- Shift reconciliation reports

**API Endpoints:**
```
GET /api/v1/reports/revenue
GET /api/v1/reports/vehicle-flow
GET /api/v1/reports/occupancy
GET /api/v1/reports/peak-hours
GET /api/v1/reports/shift-reconciliation
GET /api/v1/reports/subscriptions
GET /api/v1/reports/dashboard
```

---

## 🔄 Inter-Service Communication

### HTTP REST (Đơn giản cho MVP)
```csharp
// Parking Service → Auth Service (get user info)
public class UserServiceClient
{
    private readonly HttpClient _httpClient;
    
    public async Task<UserDto> GetUserByIdAsync(string userId)
    {
        var response = await _httpClient.GetAsync(
            $"http://localhost:5001/api/v1/users/{userId}"
        );
        return await response.Content.ReadFromJsonAsync<UserDto>();
    }
}
```

### Message Queue (Advanced - Optional)
- RabbitMQ hoặc Apache Kafka
- Event-driven architecture
- Async communication

---

## 🚀 Cách Chạy Local

### Option 1: Docker Compose (Khuyến nghị)
```bash
# 1. Copy environment variables
cp .env.example .env

# 2. Start tất cả services
docker-compose up -d

# 3. Check logs
docker-compose logs -f

# 4. Stop services
docker-compose down
```

### Option 2: Manual (Development)
```bash
# Terminal 1: MongoDB
docker run -d -p 27017:27017 --name mongo-parking mongo:latest

# Terminal 2: Auth Service
cd src/Services/Auth/Auth.API
dotnet run --urls="http://localhost:5001"

# Terminal 3: Parking Service
cd src/Services/Parking/Parking.API
dotnet run --urls="http://localhost:5002"

# Terminal 4: Payment Service
cd src/Services/Payment/Payment.API
dotnet run --urls="http://localhost:5003"

# Terminal 5: Report Service
cd src/Services/Report/Report.API
dotnet run --urls="http://localhost:5004"

# Terminal 6: API Gateway
cd src/ApiGateway
dotnet run --urls="http://localhost:5000"

# Terminal 7: Frontend
cd frontend
npm run dev
```

### Option 3: Scripts
```bash
# Bash/Git Bash
./scripts/run-local.sh

# Stop
./scripts/stop-local.sh
```

---

## 📝 Roadmap Implementation

### ✅ Phase 1: Infrastructure Setup (DONE)
- [x] Tạo tài liệu kiến trúc
- [x] Setup Ocelot configuration
- [x] Setup Docker Compose
- [x] Tạo migration guide

### 🔄 Phase 2: Shared Libraries (Next)
- [ ] Tạo `Common` project (shared DTOs, exceptions, utilities)
- [ ] Tạo `Contracts` project (interfaces, events)
- [ ] Setup JWT authentication middleware
- [ ] Setup logging & monitoring

### 📦 Phase 3: Auth Service
- [ ] Create Auth.Service project
- [ ] Implement authentication logic
- [ ] Implement user management
- [ ] Add JWT token generation
- [ ] Test through Ocelot

### 🚗 Phase 4: Parking Service
- [ ] Create Parking.Service project
- [ ] Implement building/zone/slot management
- [ ] Implement parking session logic
- [ ] Implement shift management
- [ ] Add inter-service calls to Auth

### 💳 Phase 5: Payment Service
- [ ] Create Payment.Service project
- [ ] Implement fee calculation
- [ ] Implement payment processing
- [ ] Implement subscription management
- [ ] Add inter-service calls

### 📊 Phase 6: Report Service
- [ ] Create Report.Service project
- [ ] Implement revenue reports
- [ ] Implement dashboard aggregation
- [ ] Implement analytics queries
- [ ] Setup read replicas (optional)

### 🎨 Phase 7: Frontend Integration
- [ ] Update React app to call Ocelot Gateway
- [ ] Update API endpoints
- [ ] Test end-to-end flows
- [ ] Add error handling

### 🚀 Phase 8: Deployment
- [ ] Setup CI/CD pipeline
- [ ] Deploy to Azure/AWS
- [ ] Configure production databases
- [ ] Setup monitoring & logging

---

## 📚 Tài Liệu Chi Tiết

Đọc các file sau để hiểu rõ hơn:

1. **`MICROSERVICES_SETUP.md`** - Bắt đầu từ đây! Hướng dẫn setup từng bước
2. **`docs/MICROSERVICES_ARCHITECTURE.md`** - Kiến trúc chi tiết
3. **`docs/MIGRATION_GUIDE.md`** - Cách migrate code từ monolith
4. **`docs/DEPLOYMENT_GUIDE.md`** - Hướng dẫn deploy production

---

## 🎯 Next Steps

### Ngay Bây Giờ:
1. ✅ **Đọc `MICROSERVICES_SETUP.md`** - File hướng dẫn chính
2. ✅ **Test Docker Compose** - Chạy thử các services
3. ✅ **Review Ocelot config** - Hiểu routing rules

### Tuần Này:
1. ⏳ Tạo shared libraries (Common, Contracts)
2. ⏳ Implement Auth Service
3. ⏳ Setup JWT authentication trong Ocelot

### Tuần Sau:
1. ⏳ Implement Parking Service
2. ⏳ Implement Payment Service
3. ⏳ Test inter-service communication

---

## ❓ FAQ

**Q: Có cần database riêng cho mỗi service không?**
A: Có. Mỗi service dùng 1 database riêng trên cùng một MongoDB Atlas cluster: `parking_auth_db`, `parking_main_db`, `parking_payment_db`, `parking_report_db`. Cách này đúng tinh thần microservices (isolation, scale độc lập) mà vẫn chỉ cần 1 cluster Atlas free.

**Q: Services gọi nhau như thế nào?**
A: Dùng HTTP REST với `HttpClient`. Ví dụ Parking Service cần user info → gọi Auth Service qua `http://localhost:5001/api/v1/users/{id}`

**Q: Frontend gọi API như thế nào?**
A: Frontend CHỈ gọi Ocelot Gateway (`http://localhost:5000`), không gọi trực tiếp services. Ocelot sẽ route request đến service đúng.

**Q: Có cần RabbitMQ/Kafka không?**
A: Không bắt buộc cho MVP. Dùng HTTP REST đủ. Sau này có thể thêm message queue cho event-driven architecture.

**Q: Deploy như thế nào?**
A: Đọc `docs/DEPLOYMENT_GUIDE.md`. Có thể deploy lên Azure, AWS, hoặc Docker container trên VPS.

---

## 🛠️ Tools & Technologies

- **Backend**: ASP.NET Core 8, C#
- **API Gateway**: Ocelot
- **Database**: MongoDB
- **Containerization**: Docker, Docker Compose
- **Authentication**: JWT Bearer Tokens
- **Frontend**: React, TypeScript, Vite
- **Deployment**: Azure/AWS/VPS

---

## 📞 Support

Nếu gặp vấn đề:
1. Đọc lại `MICROSERVICES_SETUP.md`
2. Check logs: `docker-compose logs -f [service-name]`
3. Verify ports không bị conflict
4. Đảm bảo MongoDB đang chạy

---

**Created:** June 16, 2026  
**Branch:** `feature/microservices-architecture`  
**Status:** Infrastructure Setup Complete ✅
