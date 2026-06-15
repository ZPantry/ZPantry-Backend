# ZPantry - Authentication Module

## 1. Tổng quan
`AuthenticationModule` là một module độc lập trong dự án ZPantry-Backend, chịu trách nhiệm xử lý các nghiệp vụ liên quan đến xác thực người dùng. Hệ thống được thiết kế theo mô hình Clean Architecture cơ bản (Controller -> Service -> Repository), giúp dễ dàng bảo trì và tái sử dụng.

Hiện tại, module đang hỗ trợ luồng **Đăng ký tài khoản mới** và **Xác thực danh tính qua email bằng mã OTP**.

## 2. Thư viện sử dụng (Dependencies)
Các thư viện chính được sử dụng trong module:
- **Entity Framework Core (EF Core 10.x):** ORM dùng để tương tác với SQL Server (gồm các package `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Tools`, `Microsoft.EntityFrameworkCore.Design`).
- **MailKit & MimeKit (v4.17.0):** Thư viện mạnh mẽ và bảo mật dùng để gửi thư điện tử / mã OTP thông qua giao thức SMTP (hiện đang cấu hình dùng Gmail).
- **Microsoft.Extensions.Configuration:** Hỗ trợ đọc các thiết lập cấu hình từ file json tùy chỉnh (`authenticationconfig.json`).
- **ASP.NET Core MVC / WebApi:** Cung cấp các nền tảng thiết kế RESTful API (Controllers).

## 3. Quy trình hoạt động (Workflow)

Dưới đây là luồng hoạt động của tính năng Đăng ký và Xác thực Email:

1. **Bước 1 (Đăng ký):** Người dùng gửi yêu cầu với thông tin (FullName, Email, Password) đến API `/register`.
2. **Bước 2 (Xử lý DB):** Hệ thống sẽ kiểm tra email. Nếu hợp lệ, hệ thống tạo tài khoản mới trong Database (với trạng thái `IsEmailConfirmed = false`), mã hóa mật khẩu, và sinh ngẫu nhiên 1 mã OTP gồm 6 chữ số có thời hạn sử dụng.
3. **Bước 3 (Gửi Email):** `EmailService` sử dụng MailKit để gửi mã OTP đó đến hòm thư thực của người dùng.
4. **Bước 4 (Xác thực):** Người dùng lấy mã từ email và gửi đến API `/verify-otp` cùng với địa chỉ email của họ.
5. **Bước 5 (Kích hoạt):** Hệ thống kiểm tra mã OTP (phải khớp mã và chưa hết hạn). Nếu thành công, hệ thống chuyển trạng thái `IsEmailConfirmed = true` và `IsActive = true`. Từ lúc này, tài khoản đã có thể đăng nhập.

## 4. Chi tiết các API

Dưới đây là các API đang được cung cấp thông qua `AuthController`. Base URL (nếu chạy local debug): `http://localhost:5207`

### 4.1. Đăng ký tài khoản (Register)
- **Endpoint:** `POST /api/auth/register`
- **Mô tả:** Đăng ký tài khoản mới và gửi mã OTP về email.
- **Payload (JSON):**
  ```json
  {
    "FullName": "Nguyen Van A",
    "Email": "email@example.com",
    "Password": "matkhau"
  }
  ```

### 4.2. Xác thực OTP (Verify OTP)
- **Endpoint:** `POST /api/auth/verify-otp`
- **Mô tả:** Xác nhận tài khoản dựa trên mã OTP được nhận trong email.
- **Payload (JSON):**
  ```json
  {
    "Email": "email@example.com",
    "OtpCode": "123456"
  }
  ```

## 5. Cấu trúc thư mục cốt lõi
- `Controllers/`: Chứa `AuthController` nhận và điều phối các HTTP request.
- `Services/`: Chứa logic nghiệp vụ (`UserService` xử lý logic tài khoản, `EmailService` xử lý gửi mail SMTP). Có chia rõ Interface và Implementation.
- `Repositories/`: Chứa `UserRepository` chịu trách nhiệm truy vấn trực tiếp đến `ZpantryDbContext` (Database).
- `DTOs/`: Chứa định dạng dữ liệu đầu vào/đầu ra (VD: `RegisterRequest`, `VerifyRequest`).
- `Entities/`: Chứa model định nghĩa các bảng trong Database (bảng `User` và `ZpantryDbContext`).
- `authenticationconfig.json`: Chứa các cấu hình riêng biệt của module (Chuỗi kết nối SQL Server, thông số cấu hình SMTP Gmail).
