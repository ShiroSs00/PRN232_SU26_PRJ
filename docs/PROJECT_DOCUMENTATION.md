# Parking Manager Project Documentation

## 1. Product Overview

Parking Manager is a parking lot management system for buildings. It supports parking staff and facility managers in handling vehicle entry and exit, managing parking slots, calculating fees, confirming payments, and viewing operational reports.

The main goal is to digitize the parking workflow, reduce manual work, prevent fee calculation mistakes, and help managers monitor parking lot status clearly.

## 2. Proposed Tech Stack

### Backend

- ASP.NET Core Web API
- .NET 8
- Microservices Architecture (Ocelot API Gateway)
- Clean Architecture
- MongoDB (MongoDB.Driver)
- MongoDB Atlas (Cloud)
- JWT Authentication
- Swagger/OpenAPI

### Frontend

- React
- TypeScript
- Vite
- Ant Design
- React Query
- Axios
- Recharts

### Deployment

- Frontend: Vercel or Netlify
- Backend: Azure App Service, Render, or VPS
- Database: MongoDB Atlas (Cloud)

## 3. User Roles

### Admin

- Manage user accounts.
- Assign roles and permissions.
- Manage system configuration.
- Monitor system status.

### Facility Manager

- Manage buildings and parking lot information.
- Manage vehicle types.
- Manage floors and zones.
- Manage parking slots.
- Manage fee policies.
- View revenue, occupancy, and vehicle flow reports.

### Parking Staff

- Check vehicles into the parking lot.
- Enter or scan plate numbers.
- Select vehicle type.
- Assign slots.
- Check vehicles out.
- Calculate fees.
- Confirm payments.
- Handle parking exceptions such as lost tickets, wrong plate numbers, overdue sessions, or wrong parking zones.

### Driver

- View parking lot information.
- View opening hours and pricing.
- View available slots.
- View current parking session.
- Pay parking fees.
- Submit feedback.

Driver features can be implemented after the MVP if time is limited.

## 4. MVP Scope

The MVP should focus on features that make the product demoable from setup to vehicle check-in/check-out and reporting.

### Must Have

- Login
- Dashboard
- Manage buildings
- Manage vehicle types
- Manage floors and zones
- Manage parking slots
- Vehicle check-in
- Vehicle check-out
- Fee calculation
- Payment confirmation
- Parking session history
- Basic reports
- User and role management

### Later Phases

- Online reservation
- Driver portal
- Feedback
- Camera license plate recognition
- Realtime slot updates
- Real payment gateway
- Mobile app
- AI slot suggestion
- Export Excel/PDF

## 5. Product Modules

### Authentication

Purpose: allow users to log in and access the system according to their roles.

Features:

- Login with email and password.
- JWT token generation.
- Role-based authorization.
- Logout.
- Get current user profile.

Roles:

- Admin
- FacilityManager
- ParkingStaff
- Driver

### Building Management

Purpose: manage building and parking lot information.

Features:

- Create building.
- Update building.
- Soft delete building.
- View building list.
- Configure opening and closing time.

Main fields:

- Id
- Name
- Address
- OpeningTime
- ClosingTime
- IsActive

### Vehicle Type Management

Purpose: manage vehicle types that are allowed in the parking lot.

Examples:

- Motorcycle
- Car
- E-bike
- Bicycle
- Small Truck

Features:

- Create vehicle type.
- Update vehicle type.
- Soft delete vehicle type.
- View vehicle type list.

### Floor and Zone Management

Purpose: manage parking floors and zones by vehicle type.

Examples:

- B1 - Motorcycle Zone
- B2 - Car Zone
- Ground Floor - Visitor Zone

Features:

- Create floor.
- Create zone.
- Assign zone to vehicle type.
- View zones by building or floor.
- Update zone active status.

### Parking Slot Management

Purpose: manage parking slots and slot status.

Slot statuses:

- Available
- Occupied
- Reserved
- Maintenance
- Locked

Features:

- Create parking slot.
- Update parking slot.
- Soft delete parking slot.
- Update slot status.
- Filter slots by building, floor, zone, vehicle type, and status.
- View available and occupied slot counts.

### Parking Session

Purpose: manage one parking session from vehicle check-in to check-out.

Session statuses:

- Active
- Completed
- Cancelled
- LostTicket
- Exception

Features:

- Check in vehicle.
- Check out vehicle.
- Search active session by plate number.
- View parking session history.
- Update session status.
- Handle lost ticket.
- Handle wrong plate number.
- Handle overdue parking.

### Fee Policy

Purpose: manage pricing and fee calculation rules.

Pricing types:

- PerTurn
- Hourly
- Daily
- Monthly

Features:

- Create fee policy.
- Update fee policy.
- Activate or deactivate fee policy.
- Calculate fee by vehicle type and parking duration.
- Configure lost ticket fee.
- Configure overtime fee.

### Payment

Purpose: record parking fee payments.

Payment statuses:

- Pending
- Paid
- Failed
- Refunded
- Cancelled

Payment methods:

- Cash
- Card
- EWallet
- Mock

Features:

- Create payment when checking out.
- Confirm payment.
- Cancel payment.
- Refund payment if needed.
- View payment history.

### Monthly Subscription

Purpose: manage registered vehicles that park on a monthly plan (for example apartment residents or company staff) so they can enter and exit without paying per turn.

Subscription statuses:

- Active
- Expired
- Suspended
- Cancelled

Features:

- Register a monthly vehicle with plate number, vehicle type, and owner information.
- Set subscription start date and end date.
- Renew a subscription.
- Suspend or cancel a subscription.
- View subscription list and expiry status.
- Check whether a plate number has an active subscription during check-in.

Main fields:

- Id
- PlateNumber
- VehicleTypeId
- BuildingId
- OwnerName
- OwnerPhone
- StartDate
- EndDate
- MonthlyFee
- Status
- IsActive

### Shift Reconciliation

Purpose: let parking staff open and close a work shift and reconcile collected cash at the end of the shift.

Shift statuses:

- Open
- Closed

Features:

- Open a shift when staff starts working.
- Record all payments collected during the shift.
- Close a shift at the end of the working period.
- Compare expected cash (sum of cash payments in the shift) with the actual counted cash.
- Record cash difference (over or short) and a note.
- View shift history and reconciliation reports.

Main fields:

- Id
- StaffUserId
- BuildingId
- OpenedAt
- ClosedAt
- ExpectedCashAmount
- CountedCashAmount
- DifferenceAmount
- Status
- Note

### Reports

Purpose: provide operational reports for facility managers.

Reports:

- Revenue by day or month.
- Vehicle check-ins and check-outs.
- Occupancy rate.
- Available slots.
- Occupied slots.
- Peak hours.
- Reports by vehicle type.
- Reports by floor and zone.
- Shift reconciliation report (expected vs counted cash, differences by staff).
- Active and expiring monthly subscriptions.

## 6. Main Business Flows

### Staff Vehicle Check-In

1. Staff opens the check-in screen.
2. Staff enters the plate number.
3. Staff selects vehicle type.
4. The system verifies whether the vehicle type is allowed.
5. The system finds a suitable available slot.
6. The system creates a parking session.
7. The system updates the slot status to `Occupied`.
8. Staff receives slot information to guide the driver.

Expected output:

```txt
Check-in successful
Plate number: 51A-12345
Vehicle type: Car
Slot: B2-C-015
Check-in time: 2026-05-20 08:30
Status: Active
```

### Staff Vehicle Check-Out

1. Staff opens the check-out screen.
2. Staff searches by plate number or session id.
3. The system displays parking session details.
4. Staff confirms check-out.
5. The system calculates parking duration.
6. The system calculates fee using the fee policy.
7. Staff confirms payment.
8. The system updates payment status to `Paid`.
9. The system updates parking session status to `Completed`.
10. The system updates slot status to `Available`.

Expected output:

```txt
Check-out successful
Plate number: 51A-12345
Check-in time: 08:30
Check-out time: 11:45
Duration: 3 hours 15 minutes
Total fee: 40,000 VND
Payment: Paid
Slot B2-C-015 changed to Available
```

### Monthly Subscription Check-In and Check-Out

1. Staff opens the check-in screen.
2. Staff enters the plate number.
3. The system checks whether the plate number has an active monthly subscription.
4. If an active subscription exists, the system creates a parking session marked as monthly and does not charge a per-turn fee.
5. If the subscription is expired or suspended, the system warns the staff and falls back to the normal per-turn check-in.
6. On check-out, a monthly session is closed without creating a payment, unless an overtime or penalty fee applies.

Expected output:

```txt
Check-in successful (Monthly)
Plate number: 51A-12345
Vehicle type: Car
Subscription: Active until 2026-12-31
Check-in time: 2026-05-20 08:30
Status: Active
Fee: 0 VND (covered by monthly plan)
```

### Capacity Full Handling

1. Staff enters the plate number and selects vehicle type on the check-in screen.
2. The system checks the remaining capacity for the matching zone or building.
3. If no available slot or remaining capacity exists for that vehicle type, the system blocks the check-in.
4. The system shows a "Parking Full" message and does not create a parking session.
5. Capacity is measured by available slot count, or by remaining capacity for capacity-based zones such as motorcycle zones.

Expected output:

```txt
Check-in blocked
Vehicle type: Motorcycle
Zone: B1 - Motorcycle Zone
Status: Parking Full
Remaining capacity: 0
```

### Staff Shift Reconciliation

1. Staff logs in and opens a new shift at the start of work.
2. All payments collected during the shift are linked to the open shift.
3. At the end of the shift, staff opens the shift close screen.
4. The system shows the expected cash amount as the sum of cash payments in the shift.
5. Staff counts the actual cash and enters the counted amount.
6. The system calculates the difference (over or short) and stores a note if needed.
7. The shift status becomes `Closed`.

Expected output:

```txt
Shift closed
Staff: nguyen.van.a
Opened: 2026-05-20 07:00
Closed: 2026-05-20 15:00
Expected cash: 1,250,000 VND
Counted cash: 1,240,000 VND
Difference: -10,000 VND (short)
```

### Facility Manager Dashboard

1. Manager logs in.
2. Manager opens the dashboard.
3. The system displays parking overview.
4. Manager views today's revenue.
5. Manager views occupancy rate.
6. Manager views check-in and check-out counts.
7. Manager views charts by day or month.

Dashboard data:

- Total slots
- Available slots
- Occupied slots
- Reserved slots
- Maintenance slots
- Today revenue
- Today check-ins
- Today check-outs
- Occupancy rate
- Peak hours

## 7. Planned API Modules

Base URL:

```txt
/api/v1
```

### Auth

```http
POST /auth/login
POST /auth/logout
GET  /auth/me
```

### Buildings

```http
GET    /buildings
GET    /buildings/{id}
POST   /buildings
PUT    /buildings/{id}
DELETE /buildings/{id}
```

### Vehicle Types

```http
GET    /vehicle-types
GET    /vehicle-types/{id}
POST   /vehicle-types
PUT    /vehicle-types/{id}
DELETE /vehicle-types/{id}
```

### Floors and Zones

```http
GET    /floors
POST   /floors
PUT    /floors/{id}
DELETE /floors/{id}

GET    /zones
POST   /zones
PUT    /zones/{id}
DELETE /zones/{id}
```

### Parking Slots

```http
GET    /parking-slots
GET    /parking-slots/{id}
POST   /parking-slots
PUT    /parking-slots/{id}
PATCH  /parking-slots/{id}/status
DELETE /parking-slots/{id}
```

### Parking Sessions

```http
GET  /parking-sessions
GET  /parking-sessions/{id}
GET  /parking-sessions/active/by-plate/{plateNumber}
POST /parking-sessions/check-in
POST /parking-sessions/{id}/check-out
POST /parking-sessions/{id}/mark-lost-ticket
POST /parking-sessions/{id}/cancel
```

### Fee Policies

```http
GET    /fee-policies
GET    /fee-policies/{id}
POST   /fee-policies
PUT    /fee-policies/{id}
DELETE /fee-policies/{id}
POST   /fee-policies/calculate
```

### Payments

```http
GET  /payments
GET  /payments/{id}
POST /payments
POST /payments/{id}/confirm
POST /payments/{id}/refund
```

### Monthly Subscriptions

```http
GET    /subscriptions
GET    /subscriptions/{id}
GET    /subscriptions/active/by-plate/{plateNumber}
POST   /subscriptions
PUT    /subscriptions/{id}
POST   /subscriptions/{id}/renew
POST   /subscriptions/{id}/suspend
POST   /subscriptions/{id}/cancel
```

### Shifts

```http
GET  /shifts
GET  /shifts/{id}
GET  /shifts/current
POST /shifts/open
POST /shifts/{id}/close
```

### Reports

```http
GET /reports/revenue
GET /reports/vehicle-flow
GET /reports/occupancy
GET /reports/peak-hours
GET /reports/shift-reconciliation
GET /reports/subscriptions
```

## 8. Database Design

### MVP Tables

- Users
- Roles
- RefreshTokens
- Buildings
- Floors
- Zones
- VehicleTypes
- ParkingSlots
- FeePolicies
- ParkingSessions
- Payments
- Subscriptions
- Shifts
- AuditLogs

Note: User roles are embedded directly in the User document as a `Roles` string array (idiomatic MongoDB), so there is no separate UserRoles join collection. The Roles collection only defines role metadata and permissions.

### Optional Tables

- Reservations
- Feedbacks
- Notifications
- CameraLogs

### Main Entity Fields

#### User

- Id
- FullName
- Email
- PasswordHash
- PhoneNumber
- Roles (string array, e.g. ["Admin"], ["ParkingStaff"])
- IsActive
- CreatedAt
- UpdatedAt

#### Role

- Id
- Name (unique, e.g. Admin, FacilityManager, ParkingStaff, Driver)
- Description
- Permissions (string array)
- IsActive

#### RefreshToken

- Id
- UserId
- Token (unique)
- ExpiresAt
- CreatedAt
- IsRevoked

#### Building

- Id
- Name
- Address
- OpeningTime
- ClosingTime
- IsActive

#### Floor

- Id
- BuildingId
- FloorNumber
- Name
- IsActive

#### Zone

- Id
- FloorId
- VehicleTypeId
- Name
- Capacity
- IsActive

#### VehicleType

- Id
- Name
- Description
- IsActive

#### ParkingSlot

- Id
- BuildingId
- FloorId
- ZoneId
- VehicleTypeId
- Code
- Status
- IsActive

#### FeePolicy

- Id
- VehicleTypeId
- Name
- PricingType
- BasePrice
- HourlyPrice
- DailyPrice
- LostTicketFee
- OvertimeFee
- IsActive
- EffectiveFrom
- EffectiveTo

#### ParkingSession

- Id
- PlateNumber
- VehicleTypeId
- ParkingSlotId
- CheckInTime
- CheckOutTime
- EntryGate
- ExitGate
- Status
- IsMonthly
- SubscriptionId
- TotalFee
- CreatedByUserId
- CompletedByUserId

#### Payment

- Id
- ParkingSessionId
- ShiftId
- Amount
- Method
- Status
- PaidAt

#### Subscription

- Id
- PlateNumber
- VehicleTypeId
- BuildingId
- OwnerName
- OwnerPhone
- StartDate
- EndDate
- MonthlyFee
- Status
- IsActive

#### Shift

- Id
- StaffUserId
- BuildingId
- OpenedAt
- ClosedAt
- ExpectedCashAmount
- CountedCashAmount
- DifferenceAmount
- Status
- Note

#### AuditLog

- Id
- UserId
- Action
- EntityName
- EntityId
- Description
- CreatedAt

> Note: AuditLog không gắn với một service nghiệp vụ cụ thể. Mỗi service tự ghi audit log vào collection `audit_logs` trong database của chính mình (ví dụ thao tác trên user ghi vào `parking_auth_db.audit_logs`).

### Id Convention

- Mỗi document dùng `_id` kiểu `ObjectId` của MongoDB làm khóa chính.
- Trong code C#, map `_id` sang property `Id` kiểu `string` (dùng `[BsonRepresentation(BsonType.ObjectId)]`) để DTO và API trả về id dạng chuỗi, tránh lộ kiểu `ObjectId` ra ngoài.
- Các trường tham chiếu chéo (ví dụ `VehicleTypeId`, `SubscriptionId`, `CreatedByUserId`) cũng lưu dạng `string` id. Vì là kiến trúc database-per-service, đây là tham chiếu logic (không có ràng buộc khóa ngoại giữa các database) — service phải tự kiểm tra tính hợp lệ khi cần.

### Indexes

Các index đã được tạo trong code hiện tại (`MongoDbInitializer.cs`):

| Collection | Field | Loại | Lý do |
|---|---|---|---|
| users | Email | unique | Đăng nhập, chặn trùng email |
| roles | Name | unique | Chặn trùng tên role |
| parking_slots | Code | unique | Mã slot là duy nhất |
| parking_slots | Status | thường | Lọc slot trống/đang dùng |
| parking_slots | BuildingId | thường | Lọc slot theo tòa nhà |
| parking_sessions | PlateNumber | thường | Tra session active theo biển số |
| parking_sessions | Status | thường | Lọc session đang active |
| zones | BuildingId | thường | Lấy zone theo tòa nhà |
| floors | BuildingId | thường | Lấy tầng theo tòa nhà |

Index cần bổ sung khi các collection sau được implement theo thiết kế target:

| Collection | Field | Loại | Lý do |
|---|---|---|---|
| refresh_tokens | Token | unique | Tra cứu / thu hồi token |
| refresh_tokens | UserId | thường | Lấy token theo user |
| subscriptions | PlateNumber | thường | Kiểm tra vé tháng lúc check-in |
| payments | ParkingSessionId | thường | Tra payment theo session |
| payments | ShiftId | thường | Tổng hợp tiền mặt theo ca |

## 9. Business Rules

- Only slots with `Available` status can be assigned to a vehicle.
- A vehicle must be assigned to a zone and slot that match its vehicle type.
- After successful check-in, `ParkingSession.Status` becomes `Active` and `ParkingSlot.Status` becomes `Occupied`.
- After successful check-out, `ParkingSession.Status` becomes `Completed`, `ParkingSlot.Status` becomes `Available`, and `Payment.Status` becomes `Paid`.
- Slots in `Maintenance` or `Locked` status cannot be assigned.
- Parking fee is calculated from vehicle type, fee policy, check-in time, check-out time, lost ticket fee, and overtime fee.
- A vehicle check-out is only completed after payment is confirmed.
- Management data such as buildings, vehicle types, floors, zones, and slots should use soft delete with `IsActive = false`.
- During check-in, the system must check whether the plate number has an active monthly subscription. If found, the session is marked as monthly and no per-turn fee is charged.
- A monthly subscription is valid only when its status is `Active` and the current date is between `StartDate` and `EndDate`. An expired or suspended subscription falls back to normal per-turn handling.
- A monthly session is closed on check-out without creating a payment, unless an overtime or penalty fee applies.
- Check-in must be blocked when there is no available slot or no remaining capacity for the matching vehicle type, and the system shows a "Parking Full" message. Capacity is measured by available slot count, or by remaining capacity for capacity-based zones.
- All cash payments must be linked to the staff member's currently open shift.
- A staff member can have only one open shift at a time. A new shift cannot be opened until the previous one is closed.
- On shift close, the system records the difference between expected cash (sum of cash payments in the shift) and the actual counted cash. A note is required when a difference exists.

## 10. Suggested Screens

### Public/Auth

- Login page
- Forgot password page, optional

### Admin/Manager

- Dashboard overview
- Revenue chart
- Occupancy chart
- Vehicle flow chart
- Recent parking sessions

### Management Pages

- Buildings
- Vehicle Types
- Floors
- Zones
- Parking Slots
- Fee Policies
- Users
- Monthly Subscriptions
- Shift Reconciliation Report

### Staff Pages

- Check-in
- Check-out
- Active Sessions
- Session Detail
- Exception Handling
- Open/Close Shift

## 11. Development Priority

### Priority 1

- Project setup
- Database connection
- JWT authentication
- Role authorization
- Building CRUD
- Vehicle Type CRUD
- Floor/Zone CRUD
- Parking Slot CRUD
- Check-in
- Check-out
- Fee calculation
- Payment confirmation
- Capacity full check on check-in
- Dashboard overview

### Priority 2

- Parking session history
- Reports
- User management
- Audit log
- Search, filter, and pagination
- Monthly subscription management
- Shift reconciliation

### Priority 3

- Reservation
- Feedback
- Realtime dashboard
- Camera integration
- Export Excel/PDF
- Payment gateway

## 12. Demo Script

### Demo 1: Manager Setup

1. Log in as Facility Manager.
2. Create a building.
3. Create vehicle types.
4. Create floors.
5. Create zones.
6. Create parking slots.
7. Create fee policies.

### Demo 2: Staff Check-In

1. Log in as Parking Staff.
2. Open the check-in screen.
3. Enter plate number.
4. Select vehicle type.
5. Click check-in.
6. The system creates a parking session and assigns a slot.

### Demo 3: Staff Check-Out

1. Open the check-out screen.
2. Search vehicle by plate number.
3. View parking session details.
4. Click check-out.
5. The system calculates fee.
6. Confirm payment.
7. The slot becomes available again.

### Demo 4: Manager Reports

1. Log in as Facility Manager.
2. Open Dashboard.
3. View total slots.
4. View available and occupied slots.
5. View today's revenue.
6. View revenue chart.

## 13. Final MVP Goal

The MVP is complete when the system can:

- Manage parking lot data.
- Track parking slots.
- Check vehicles in.
- Check vehicles out.
- Calculate parking fees.
- Record payments.
- Show dashboard reports.
- Authorize users by role.

