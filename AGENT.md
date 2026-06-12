# Cẩm Nang Phát Triển Backend - Parking Manager

Cẩm nang này hướng dẫn đội ngũ Backend (BE) cách nắm bắt cấu trúc dự án, tuân thủ các quy chuẩn viết code và triển khai các tính năng tiếp theo theo đúng kiến trúc Clean Architecture của hệ thống.

---

## 1. Tổng Quan Tech Stack & Cơ Sở Dữ Liệu
* **Framework:** .NET 8.0 & ASP.NET Core Web API.
* **Database:** MongoDB (Sử dụng thư viện `MongoDB.Driver` chính thức).
* **Bảo mật:** JWT Authentication & Phân quyền theo vai trò (Role-based Authorization).
* **Mã hóa mật khẩu:** BCrypt.Net-Next.

---

## 2. Cấu Trúc Dự Án (Clean Architecture)
Dự án được chia thành 4 layer chính tương ứng với các project:

```txt
ParkingSystem.sln
│
├── ParkingSystem.Domain/             # Thực thể cốt lõi, Enums, Constants (Không phụ thuộc vào lớp khác)
│   ├── Constants/                     # Hằng số hệ thống (UserRoles, ParkingSlotStatuses,...)
│   ├── Enums/                         # Enum hệ thống (UserRole)
│   └── Entities/                      # Các Document Entity lưu trữ MongoDB (User, Building, Floor,...)
│
├── ParkingSystem.Application/        # Logic nghiệp vụ trừu tượng, Interfaces, DTOs, Validation
│   ├── Common/                        # Cấu trúc chung (ApiResponse, JwtSettings)
│   ├── DTOs/                          # Các mẫu dữ liệu Request/Response của API
│   ├── Services/                      # Interface định nghĩa các dịch vụ (IUserService, ITokenService,...)
│   └── Validation/                    # Bộ kiểm duyệt dữ liệu tự định nghĩa (ValidRolesAttribute)
│
├── ParkingSystem.Infrastructure/     # Persistence (MongoDB), hiện thực hóa Services, Khởi tạo dữ liệu
│   ├── Persistence/                   # MongoDbContext, MongoDbInitializer (Seeder & Indexer)
│   ├── Services/                      # Hiện thực hóa các interface dịch vụ (UserService, TokenService,...)
│   └── DependencyInjection.cs         # Đăng ký Service & cấu hình JWT Authentication
│
└── ParkingSystem.API/                 # Controllers, Cấu hình HTTP Pipeline (Middleware, Filters)
    ├── Controllers/                   # Các API endpoints nhận request và trả response
    ├── Filters/                       # ValidationFilter tự động bắt lỗi dữ liệu đầu vào
    ├── Middlewares/                   # ExceptionMiddleware xử lý lỗi runtime toàn cục
    ├── Extensions/                    # ServiceExtensions (Swagger JWT, CORS) giúp rút gọn Program.cs
    └── Program.cs                     # Entrypoint khởi chạy ứng dụng
```

---

## 3. Thiết Lập & Chạy Thử
1. **Cấu hình Connection String:**
   Tạo file `ParkingSystem.API/appsettings.Local.json` ở local của bạn (file này đã được đưa vào `.gitignore` để tránh bị commit lộ mật khẩu):
   ```json
   {
     "MongoDbSettings": {
       "ConnectionString": "mongodb://localhost:27017",
       "DatabaseName": "ParkingManagerDb"
     }
   }
   ```
2. **Khởi động dự án:**
   Mở terminal tại thư mục root của dự án và chạy:
   ```bash
   dotnet run --project ParkingSystem.API
   ```
   *Ứng dụng sẽ tự động khởi tạo database, cấu hình các index duy nhất, và seed sẵn các tài khoản mẫu.*

---

## 4. Các Quy Chuẩn Viết Code Chung (Bắt Buộc Tuân Thủ)

### 4.1. Định dạng Response chuẩn (`ApiResponse<T>`)
Tất cả các endpoint API phải trả về dữ liệu được bọc trong định dạng `ApiResponse<T>` để Frontend dễ dàng xử lý đồng nhất:
* **Khi thành công (HTTP 200/201):**
  ```csharp
  return Ok(ApiResponse.Ok("Thông báo thành công", data));
  ```
* **Khi thất bại (HTTP 400/401/403/404/500):**
  ```csharp
  return BadRequest(ApiResponse.Fail("Nội dung lỗi chi tiết", errorsObject));
  ```

### 4.2. Xử lý Validation đầu vào tự động
* Các Request DTO cần khai báo các thuộc tính ràng buộc dữ liệu đầu vào bằng `DataAnnotations` (như `[Required]`, `[EmailAddress]`, `[MinLength]`).
* Bạn **không cần** viết code `if (!ModelState.IsValid)` thủ công trong Controller.
* **`ValidationFilter`** đã được cấu hình toàn cục. Nếu dữ liệu gửi lên sai định dạng, Filter sẽ tự động đánh chặn và trả về lỗi `HTTP 400 Bad Request` theo format `ApiResponse` chuẩn.

### 4.3. Xử lý Lỗi Hệ Thống & Phân Quyền (Exception Handling)
* Trong các Service, nếu gặp lỗi logic hoặc không có quyền truy cập, hãy **quăng ngoại lệ trực tiếp**:
  - Lỗi logic thông thường: `throw new Exception("Email này đã được đăng ký.");` -> Middleware trả về HTTP 500.
  - Lỗi phân quyền/sở hữu tài nguyên: `throw new UnauthorizedAccessException("Bạn không có quyền sửa tài nguyên này.");` -> Middleware tự động trả về **HTTP 403 Forbidden** với response chuẩn.
* **`ExceptionMiddleware`** sẽ tự động bắt các lỗi này để trả về HTTP status code tương ứng và bọc trong JSON dạng `ApiResponse.Fail` bảo mật.

### 4.4. Lấy Thông Tin Người Dùng Hiện Tại (User Context)
* **Tuyệt đối không** gọi `User.FindFirst(...)` hoặc `User.IsInRole(...)` trực tiếp trong Controller để giữ cho Controller luôn mỏng (thin controller) và độc lập.
* Thay vào đó, hãy inject **`ICurrentUserService`** vào Service hoặc Controller của bạn để lấy thông tin của người dùng đang đăng nhập:
  - `_currentUserService.UserId`: Lấy ID của user hiện tại.
  - `_currentUserService.Email`: Lấy Email của user hiện tại.
  - `_currentUserService.IsInRole(UserRoles.Admin)`: Kiểm tra xem user hiện tại có vai trò nào đó không.

### 4.5. Cơ chế Xóa Mềm (Soft Delete)
* Theo quy định nghiệp vụ, các dữ liệu quản lý (Tòa nhà, Loại xe, Tầng, Khu vực, Slot, Người dùng) **không được phép xóa cứng** khỏi database.
* Hãy sử dụng cờ `IsActive = false` và cập nhật ngày `UpdatedAt`.

---

## 5. Hướng Dẫn Từng Bước Viết Tính Năng Mới
Khi nhận nhiệm vụ làm một module mới (ví dụ: Quản lý tòa nhà - Building CRUD), hãy làm theo các bước sau:

### Bước 1: Định nghĩa Thực thể (Domain Layer)
Tạo file entity kế thừa cấu trúc MongoDB trong `ParkingSystem.Domain/Entities/Building.cs`.

### Bước 2: Tạo các DTOs (Application Layer)
Tạo các lớp DTO trong `ParkingSystem.Application/DTOs/`:
* `BuildingDto` (Trả về client)
* `CreateBuildingDto` (Validate dữ liệu gửi lên khi tạo mới)
* `UpdateBuildingDto` (Validate dữ liệu gửi lên khi cập nhật)

### Bước 3: Tạo Service Interface (Application Layer)
Tạo interface trong `ParkingSystem.Application/Services/IBuildingService.cs` định nghĩa các phương thức CRUD.

### Bước 4: Hiện thực hóa Service (Infrastructure Layer)
Tạo lớp hiện thực hóa interface trong `ParkingSystem.Infrastructure/Services/BuildingService.cs` để query MongoDB.

### Bước 5: Đăng ký Dependency Injection (Infrastructure Layer)
Mở [DependencyInjection.cs](file:///d:/FPT/PRN232/PRN232_SU26_PRJ/ParkingSystem.Infrastructure/DependencyInjection.cs) và đăng ký service của bạn:
```csharp
services.AddSingleton<IBuildingService, BuildingService>();
```

### Bước 6: Viết API Controller (API Layer)
Tạo `BuildingController.cs` trong `ParkingSystem.API/Controllers/` kế thừa `ControllerBase`:
* Sử dụng `[Authorize]` để bảo vệ endpoint.
* Ví dụ chỉ cho Manager truy cập: `[Authorize(Roles = UserRoles.FacilityManager)]`.
* Gọi các hàm từ `IBuildingService` và bọc kết quả trả về trong `ApiResponse`.
