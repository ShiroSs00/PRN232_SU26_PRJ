# Parking Manager System - Architecture Diagrams

## 1. High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│                    Client Layer                                 │
│                                                                 │
│   ┌─────────────────────────────────────────────────────┐     │
│   │         React Application (Port 5173)                │     │
│   │                                                       │     │
│   │  - Building Management UI                            │     │
│   │  - Vehicle Check-in/Check-out UI                     │     │
│   │  - Payment Processing UI                             │     │
│   │  - Reports & Dashboard                               │     │
│   └─────────────────────────────────────────────────────┘     │
│                                                                 │
└─────────────────────────┬───────────────────────────────────────┘
                          │
                          │ HTTPS/REST
                          │
                          ▼
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│                    API Gateway Layer                            │
│                                                                 │
│   ┌─────────────────────────────────────────────────────┐     │
│   │         Ocelot API Gateway (Port 5000)               │     │
│   │                                                       │     │
│   │  ✓ Request Routing                                   │     │
│   │  ✓ JWT Authentication                                │     │
│   │  ✓ Rate Limiting                                     │     │
│   │  ✓ Load Balancing                                    │     │
│   │  ✓ API Aggregation                                   │     │
│   │  ✓ CORS Configuration                                │     │
│   └─────────────────────────────────────────────────────┘     │
│                                                                 │
└───────┬───────────────┬───────────────┬───────────────┬─────────┘
        │               │               │               │
        │               │               │               │
        ▼               ▼               ▼               ▼
┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌──────────────┐
│              │ │              │ │              │ │              │
│     Auth     │ │   Parking    │ │   Payment    │ │    Report    │
│   Service    │ │   Service    │ │   Service    │ │   Service    │
│  Port 5001   │ │  Port 5002   │ │  Port 5003   │ │  Port 5004   │
│              │ │              │ │              │ │              │
└──────┬───────┘ └──────┬───────┘ └──────┬───────┘ └──────┬───────┘
       │                │                │                │
       │                │                │                │
       └────────────────┴────────────────┴────────────────┘
                        │
                        │
                        ▼
        ┌───────────────────────────────────────┐
        │                                       │
        │        Database Layer                 │
        │                                       │
        │  ┌──────────────────────────────┐    │
        │  │     MongoDB Cluster          │    │
        │  │                              │    │
        │  │  - parking_auth_db           │    │
        │  │  - parking_main_db           │    │
        │  │  - parking_payment_db        │    │
        │  │  - parking_report_db         │    │
        │  │                              │    │
        │  └──────────────────────────────┘    │
        │                                       │
        └───────────────────────────────────────┘
```

---

## 2. Service Boundary Context

```
┌─────────────────────────────────────────────────────────────────┐
│                        Auth Context                             │
│                                                                 │
│  Entities:                                                      │
│  - User (Roles nhúng dạng string[])                             │
│  - Role (metadata + permissions)                                │
│  - RefreshToken                                                 │
│                                                                 │
│  Responsibilities:                                              │
│  - User authentication & authorization                          │
│  - JWT token management                                         │
│  - User CRUD operations                                         │
│  - Role & permission management                                 │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                      Parking Context                            │
│                                                                 │
│  Entities:                                                      │
│  - Building                                                     │
│  - Floor, Zone                                                  │
│  - VehicleType                                                  │
│  - ParkingSlot                                                  │
│  - ParkingSession                                               │
│  - Shift                                                        │
│                                                                 │
│  Responsibilities:                                              │
│  - Physical infrastructure management                           │
│  - Vehicle check-in/check-out                                   │
│  - Slot availability tracking                                   │
│  - Shift management                                             │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                      Payment Context                            │
│                                                                 │
│  Entities:                                                      │
│  - FeePolicy                                                    │
│  - Payment                                                      │
│  - Subscription                                                 │
│                                                                 │
│  Responsibilities:                                              │
│  - Fee calculation                                              │
│  - Payment processing                                           │
│  - Monthly subscription management                              │
│  - Payment reconciliation                                       │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                      Report Context                             │
│                                                                 │
│  Read Models:                                                   │
│  - RevenueReport                                                │
│  - OccupancyReport                                              │
│  - VehicleFlowReport                                            │
│  - ShiftReconciliationReport                                    │
│                                                                 │
│  Responsibilities:                                              │
│  - Data aggregation from other services                         │
│  - Analytics & reporting                                        │
│  - Dashboard metrics                                            │
└─────────────────────────────────────────────────────────────────┘
```

---

## 3. Request Flow - Vehicle Check-In

```
┌──────────┐                                                    
│  Client  │                                                    
└────┬─────┘                                                    
     │                                                          
     │ POST /api/v1/parking/parking-sessions/check-in         
     │ { plateNumber, vehicleTypeId, staffUserId }            
     │                                                          
     ▼                                                          
┌────────────────┐                                              
│ Ocelot Gateway │                                              
│  Port 5000     │                                              
└────┬───────────┘                                              
     │                                                          
     │ 1. Verify JWT Token                                     
     │ 2. Rate Limit Check                                     
     │ 3. Route to Parking Service                             
     │                                                          
     ▼                                                          
┌──────────────────┐                                            
│ Parking Service  │                                            
│   Port 5002      │                                            
└────┬─────────────┘                                            
     │                                                          
     │ 4. Get User Info                                        
     │ ──────────────────────────────────┐                    
     │                                    │                    
     │                                    ▼                    
     │                          ┌─────────────────┐           
     │                          │  Auth Service   │           
     │                          │   Port 5001     │           
     │                          └────────┬────────┘           
     │                                   │                    
     │ 5. Return UserDto ◄───────────────┘                    
     │                                                          
     │ 6. Check Subscription Status                            
     │ ──────────────────────────────────┐                    
     │                                    │                    
     │                                    ▼                    
     │                          ┌──────────────────┐          
     │                          │ Payment Service  │          
     │                          │   Port 5003      │          
     │                          └────────┬─────────┘          
     │                                   │                    
     │ 7. Return SubscriptionDto ◄───────┘                    
     │                                                          
     │ 8. Find Available Slot (DB Query)                      
     │ 9. Create ParkingSession (DB Insert)                   
     │ 10. Update Slot Status (DB Update)                     
     │                                                          
     │ 11. Return CheckInResponse                              
     │                                                          
     ▼                                                          
┌────────────────┐                                              
│ Ocelot Gateway │                                              
└────┬───────────┘                                              
     │                                                          
     │ 12. Return Response to Client                           
     │                                                          
     ▼                                                          
┌──────────┐                                                    
│  Client  │                                                    
└──────────┘                                                    
```

---

## 4. Request Flow - Vehicle Check-Out & Payment

```
┌──────────┐                                                    
│  Client  │                                                    
└────┬─────┘                                                    
     │                                                          
     │ POST /api/v1/parking/parking-sessions/{id}/check-out   
     │                                                          
     ▼                                                          
┌────────────────┐                                              
│ Ocelot Gateway │                                              
│  Port 5000     │                                              
└────┬───────────┘                                              
     │                                                          
     ▼                                                          
┌──────────────────┐                                            
│ Parking Service  │                                            
│   Port 5002      │                                            
└────┬─────────────┘                                            
     │                                                          
     │ 1. Get ParkingSession (DB Query)                        
     │ 2. Calculate Duration                                   
     │                                                          
     │ 3. Calculate Fee                                        
     │ ──────────────────────────────────┐                    
     │                                    │                    
     │                                    ▼                    
     │                          ┌──────────────────┐          
     │                          │ Payment Service  │          
     │                          │   Port 5003      │          
     │                          └────────┬─────────┘          
     │                                   │                    
     │                          4. Return FeeAmount            
     │ 5. Return Fee ◄───────────────────┘                    
     │                                                          
     │ 6. Create Payment                                       
     │ ──────────────────────────────────┐                    
     │                                    │                    
     │                                    ▼                    
     │                          ┌──────────────────┐          
     │                          │ Payment Service  │          
     │                          │   Port 5003      │          
     │                          └────────┬─────────┘          
     │                                   │                    
     │ 7. Return PaymentDto ◄────────────┘                    
     │                                                          
     │ 8. Update Session Status (DB Update)                   
     │ 9. Update Slot Status to Available (DB Update)         
     │ 10. Return CheckOutResponse                             
     │                                                          
     ▼                                                          
┌────────────────┐                                              
│ Ocelot Gateway │                                              
└────┬───────────┘                                              
     │                                                          
     ▼                                                          
┌──────────┐                                                    
│  Client  │                                                    
└──────────┘                                                    
```

---

## 5. Database Schema per Service

### Auth Service - parking_auth_db

```
┌─────────────────────────────────────────┐
│               Users                     │
├─────────────────────────────────────────┤
│ _id: ObjectId (PK)                      │
│ FullName: string                        │
│ Email: string (unique index)            │
│ PasswordHash: string                    │
│ PhoneNumber: string                     │
│ Roles: string[]   (embedded, vd ["Admin"])│
│ IsActive: bool                          │
│ CreatedAt: DateTime                     │
│ UpdatedAt: DateTime                     │
└─────────────────────────────────────────┘
  Roles nhúng thẳng vào User (idiomatic MongoDB)
  → JWT claims lấy trực tiếp từ User.Roles,
    không cần join collection UserRoles.

┌─────────────────────────────────────────┐
│               Roles                     │
├─────────────────────────────────────────┤
│ _id: ObjectId (PK)                      │
│ Name: string (unique index)             │
│ Description: string                     │
│ Permissions: string[]                   │
│ IsActive: bool                          │
└─────────────────────────────────────────┘
  Bảng tra cứu định nghĩa role + permission.
  User.Roles tham chiếu tới Roles.Name.

┌─────────────────────────────────────────┐
│           RefreshTokens                 │
├─────────────────────────────────────────┤
│ _id: ObjectId (PK)                      │
│ UserId: ObjectId (FK → Users)           │
│ Token: string (unique index)            │
│ ExpiresAt: DateTime                     │
│ CreatedAt: DateTime                     │
│ IsRevoked: bool                         │
└─────────────────────────────────────────┘
```

### Parking Service - parking_main_db

```
┌─────────────────────────────────────────┐
│             Buildings                   │
├─────────────────────────────────────────┤
│ _id: ObjectId (PK)                      │
│ Name: string                            │
│ Address: string                         │
│ OpeningTime: TimeSpan                   │
│ ClosingTime: TimeSpan                   │
│ IsActive: bool                          │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│             VehicleTypes                │
├─────────────────────────────────────────┤
│ _id: ObjectId (PK)                      │
│ Name: string                            │
│ Description: string                     │
│ IsActive: bool                          │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│               Floors                    │
├─────────────────────────────────────────┤
│ _id: ObjectId (PK)                      │
│ BuildingId: ObjectId (FK → Buildings)   │
│ FloorNumber: int                        │
│ Name: string                            │
│ IsActive: bool                          │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│               Zones                     │
├─────────────────────────────────────────┤
│ _id: ObjectId (PK)                      │
│ FloorId: ObjectId (FK → Floors)         │
│ VehicleTypeId: ObjectId (FK)            │
│ Name: string                            │
│ Capacity: int                           │
│ IsActive: bool                          │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│             ParkingSlots                │
├─────────────────────────────────────────┤
│ _id: ObjectId (PK)                      │
│ BuildingId: ObjectId (FK → Buildings)   │
│ FloorId: ObjectId (FK → Floors)         │
│ ZoneId: ObjectId (FK → Zones)           │
│ VehicleTypeId: ObjectId (FK)            │
│ Code: string (unique)                   │
│ Status: string (enum)                   │
│ IsActive: bool                          │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│          ParkingSessions                │
├─────────────────────────────────────────┤
│ _id: ObjectId (PK)                      │
│ PlateNumber: string                     │
│ VehicleTypeId: ObjectId (FK)            │
│ ParkingSlotId: ObjectId (FK)            │
│ CheckInTime: DateTime                   │
│ CheckOutTime: DateTime?                 │
│ Status: string (enum)                   │
│ IsMonthly: bool                         │
│ SubscriptionId: ObjectId? (FK)          │
│ TotalFee: decimal                       │
│ CreatedByUserId: ObjectId (FK)          │
│ CompletedByUserId: ObjectId? (FK)       │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│               Shifts                    │
├─────────────────────────────────────────┤
│ _id: ObjectId (PK)                      │
│ StaffUserId: ObjectId (FK)              │
│ BuildingId: ObjectId (FK)               │
│ OpenedAt: DateTime                      │
│ ClosedAt: DateTime?                     │
│ ExpectedCashAmount: decimal             │
│ CountedCashAmount: decimal?             │
│ DifferenceAmount: decimal?              │
│ Status: string (enum)                   │
│ Note: string                            │
└─────────────────────────────────────────┘
```

### Payment Service - parking_payment_db

```
┌─────────────────────────────────────────┐
│            FeePolicies                  │
├─────────────────────────────────────────┤
│ _id: ObjectId (PK)                      │
│ VehicleTypeId: ObjectId (FK)            │
│ Name: string                            │
│ PricingType: string (enum)              │
│ BasePrice: decimal                      │
│ HourlyPrice: decimal?                   │
│ DailyPrice: decimal?                    │
│ LostTicketFee: decimal                  │
│ OvertimeFee: decimal                    │
│ IsActive: bool                          │
│ EffectiveFrom: DateTime                 │
│ EffectiveTo: DateTime?                  │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│              Payments                   │
├─────────────────────────────────────────┤
│ _id: ObjectId (PK)                      │
│ ParkingSessionId: ObjectId (FK)         │
│ ShiftId: ObjectId? (FK)                 │
│ Amount: decimal                         │
│ Method: string (enum)                   │
│ Status: string (enum)                   │
│ PaidAt: DateTime?                       │
│ CreatedAt: DateTime                     │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│           Subscriptions                 │
├─────────────────────────────────────────┤
│ _id: ObjectId (PK)                      │
│ PlateNumber: string                     │
│ VehicleTypeId: ObjectId (FK)            │
│ BuildingId: ObjectId (FK)               │
│ OwnerName: string                       │
│ OwnerPhone: string                      │
│ StartDate: DateTime                     │
│ EndDate: DateTime                       │
│ MonthlyFee: decimal                     │
│ Status: string (enum)                   │
│ IsActive: bool                          │
└─────────────────────────────────────────┘
```

---

## 6. Deployment Architecture

### Development Environment

```
┌─────────────────────────────────────────────────────────────┐
│                    Developer Machine                        │
│                                                             │
│  4 Services + Ocelot Gateway chạy bằng `dotnet run`         │
│  (hoặc docker-compose, KHÔNG bao gồm MongoDB)               │
│                                                             │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐    │
│  │  Auth    │  │ Parking  │  │ Payment  │  │  Report  │    │
│  │ :5001    │  │ :5002    │  │ :5003    │  │ :5004    │    │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘    │
│                                                             │
│  ┌─────────────────────────────────────┐                   │
│  │     Ocelot Gateway  :5000           │                   │
│  └─────────────────────────────────────┘                   │
│                            │                                │
└─────────────────────────────────────────────────────────────┘
```

### Production Environment (Azure Example)

```
┌───────────────────────────────────────────────────────────────┐
│                      Azure Cloud                              │
│                                                               │
│  ┌─────────────────────────────────────────────────────┐     │
│  │          Azure Front Door (CDN + SSL)               │     │
│  └────────────────────┬────────────────────────────────┘     │
│                       │                                       │
│  ┌────────────────────▼────────────────────────────────┐     │
│  │      Azure Application Gateway (WAF)                │     │
│  └────────────────────┬────────────────────────────────┘     │
│                       │                                       │
│  ┌────────────────────▼────────────────────────────────┐     │
│  │      Azure Kubernetes Service (AKS)                 │     │
│  │                                                      │     │
│  │  ┌───────────┐  ┌───────────┐  ┌───────────┐      │     │
│  │  │  Gateway  │  │   Auth    │  │  Parking  │      │     │
│  │  │    Pod    │  │    Pod    │  │    Pod    │      │     │
│  │  └───────────┘  └───────────┘  └───────────┘      │     │
│  │                                                      │     │
│  │  ┌───────────┐  ┌───────────┐                      │     │
│  │  │  Payment  │  │  Report   │                      │     │
│  │  │    Pod    │  │    Pod    │                      │     │
│  │  └───────────┘  └───────────┘                      │     │
│  │                                                      │     │
│  └──────────────────────────────────────────────────────┘     │
│                       │                                       │
│  ┌────────────────────▼────────────────────────────────┐     │
│  │      Azure Cosmos DB for MongoDB                    │     │
│  │                                                      │     │
│  │  - parking_auth_db (replica set)                    │     │
│  │  - parking_main_db (replica set)                    │     │
│  │  - parking_payment_db (replica set)                 │     │
│  │  - parking_report_db (replica set)                  │     │
│  └──────────────────────────────────────────────────────┘     │
│                                                               │
│  ┌──────────────────────────────────────────────────────┐    │
│  │    Azure Monitor + Application Insights              │    │
│  └──────────────────────────────────────────────────────┘    │
│                                                               │
└───────────────────────────────────────────────────────────────┘
```

---

## 7. Security Architecture

```
┌──────────────────────────────────────────────────────────┐
│                    Security Layers                       │
└──────────────────────────────────────────────────────────┘

Layer 1: Network Security
┌──────────────────────────────────────────────────────────┐
│  - HTTPS/TLS 1.3 Encryption                              │
│  - WAF (Web Application Firewall)                        │
│  - DDoS Protection                                       │
│  - IP Whitelisting                                       │
└──────────────────────────────────────────────────────────┘

Layer 2: API Gateway Security
┌──────────────────────────────────────────────────────────┐
│  - JWT Authentication                                    │
│  - Rate Limiting (100 req/min per user)                 │
│  - CORS Policy                                           │
│  - Request/Response Validation                           │
└──────────────────────────────────────────────────────────┘

Layer 3: Service-Level Security
┌──────────────────────────────────────────────────────────┐
│  - Role-Based Access Control (RBAC)                      │
│  - Service-to-Service Authentication                     │
│  - Input Sanitization                                    │
│  - SQL/NoSQL Injection Prevention                        │
└──────────────────────────────────────────────────────────┘

Layer 4: Data Security
┌──────────────────────────────────────────────────────────┐
│  - Password Hashing (BCrypt)                             │
│  - Sensitive Data Encryption at Rest                     │
│  - Database Access Control                               │
│  - Audit Logging                                         │
└──────────────────────────────────────────────────────────┘
```

---

## 8. Monitoring & Observability

```
┌───────────────────────────────────────────────────────────┐
│                  Observability Stack                      │
│                                                           │
│  ┌─────────────────────────────────────────────────┐     │
│  │              Logging                            │     │
│  │  - Serilog (Structured Logging)                │     │
│  │  - ELK Stack (Elasticsearch, Logstash, Kibana) │     │
│  │  - Log Levels: Debug, Info, Warning, Error     │     │
│  └─────────────────────────────────────────────────┘     │
│                                                           │
│  ┌─────────────────────────────────────────────────┐     │
│  │              Monitoring                         │     │
│  │  - Prometheus (Metrics Collection)             │     │
│  │  - Grafana (Dashboards)                        │     │
│  │  - Application Insights (Azure)                │     │
│  └─────────────────────────────────────────────────┘     │
│                                                           │
│  ┌─────────────────────────────────────────────────┐     │
│  │           Distributed Tracing                   │     │
│  │  - OpenTelemetry                               │     │
│  │  - Jaeger                                      │     │
│  │  - Request ID Propagation                      │     │
│  └─────────────────────────────────────────────────┘     │
│                                                           │
│  ┌─────────────────────────────────────────────────┐     │
│  │             Health Checks                       │     │
│  │  - /health endpoint per service                │     │
│  │  - Database connectivity check                 │     │
│  │  - Dependency health check                     │     │
│  └─────────────────────────────────────────────────┘     │
│                                                           │
└───────────────────────────────────────────────────────────┘
```

---

## Legend

```
┌─────┐
│  │     = Component/Service
└─────┘

───►    = HTTP/REST Call
═══►    = Database Connection
- - ►   = Event/Message
```

---

**Created:** June 16, 2026  
**Version:** 1.0  
**Status:** Architecture Design Complete
