# 📋 Tóm Tắt Thay Đổi Mã Nguồn & Cập Nhật Hệ Thống (CHANGELOG)

---

## 1. 💡 Giới Thiệu

Tài liệu này tổng hợp toàn bộ các thay đổi mới nhất trong hệ thống **ZPantry-Backend**. Các cải tiến tập trung vào việc hoàn thiện tính năng CRUD cho các module cốt lõi (Người dùng & Nguyên liệu), điều chỉnh nghiệp vụ phân quyền bảo mật chặt chẽ theo vai trò thực tế, cùng với việc nâng cấp đồng bộ hạ tầng triển khai đa container trên Docker.

---

## 2. 🔄 Những Phần Thay Đổi (Nghiệp Vụ & API)

### A. Quản Lý Người Dùng (User Management)

> [!IMPORTANT]
> Nghiệp vụ được tái cấu trúc để đảm bảo tính riêng tư dữ liệu và phân chia quyền hạn rõ ràng giữa Quản trị viên (Admin) và Người dùng sở hữu tài khoản (Account Owner).

1. **Điều chỉnh Nghiệp vụ (Business Logic)**:
   * **Loại bỏ luồng Tạo tài khoản từ Admin**: Admin không được phép tự tạo tài khoản người dùng thủ công. Người dùng mới bắt buộc phải đi qua luồng Đăng ký (`POST /api/auth/register`) và xác thực OTP qua Gmail.
   * **Bảo vệ quyền riêng tư hồ sơ cá nhân**: Admin không có quyền chỉnh sửa thông tin cá nhân của người dùng. Chỉ người dùng sở hữu tài khoản (kiểm tra khớp ID từ JWT Token) mới được phép thay đổi thông tin của chính mình.
   * **Cập nhật từng phần (Partial Update)**: Giao thức cập nhật cho phép giữ nguyên các trường nếu bỏ trống hoặc không gửi lên (`null`).

2. **Chi tiết API & Mã Nguồn**:
   * **`GET /api/users`** *(Quyền: Admin)*: Lấy danh sách người dùng phân trang, tìm kiếm từ khóa theo Tên/Email và lọc theo Vai trò hoặc Trạng thái.
   * **`GET /api/users/{id}`** *(Quyền: Admin)*: Xem chi tiết thông tin của một người dùng bất kỳ.
   * **`PUT /api/users/{id}`** *(Quyền: Chính chủ)*: Cập nhật thông tin cá nhân. Thu hẹp DTO chỉ cho phép sửa `FullName`, `AvatarUrl`, và `Password` (bỏ qua các trường quản trị như `Role`, `IsActive`, `IsEmailConfirmed`).
   * **`DELETE /api/users/{id}`** *(Quyền: Admin)*: Xóa mềm tài khoản người dùng (`is_deleted = true`).
   * **File liên quan**: [UsersController.cs](file:///d:/Proj/ZPantry-Backend/AuthenticationModule/Controllers/UsersController.cs), [UserDtos.cs](file:///d:/Proj/ZPantry-Backend/AuthenticationModule/DTOs/UserDtos.cs), [UserService.cs](file:///d:/Proj/ZPantry-Backend/AuthenticationModule/Services/Implementations/UserService.cs).

---

### B. Quản Lý Nguyên Liệu (Ingredient Management)

> [!NOTE]
> Phân định nghiệp vụ: Các thao tác CRUD (thêm/sửa/xóa nguyên liệu) đã được loại bỏ khỏi `IngredientsController` và `IIngredientService` để tránh dư thừa vì đã được quản lý qua luồng dữ liệu chuẩn có sẵn. Module chỉ tập trung cung cấp các endpoint tra cứu nhanh, linh hoạt cho người dùng.

1. **Điều chỉnh Nghiệp vụ (Business Logic)**:
   * **Loại bỏ API CRUD thừa**: Đã xóa các endpoint `POST`, `PUT`, `DELETE` nguyên liệu khỏi controller và service để tránh trùng lặp nghiệp vụ.
   * Cung cấp API lấy toàn bộ danh sách nguyên liệu thuần (không áp dụng bộ lọc hay phân trang) phục vụ cho các logic truy xuất nhanh, dropdowns hoặc cache phía Frontend.
   * Hỗ trợ tìm kiếm thông minh theo từ khóa (`SearchTerm`) và lọc theo danh mục (`Category`) với chế độ phân trang.

2. **Chi tiết API & Mã Nguồn**:
   * **`GET /api/ingredients`** *(Quyền: Public/User)*: Lấy danh sách nguyên liệu phân trang và bộ lọc.
   * **`GET /api/ingredients/all`** *(Quyền: Public/User)*: Lấy toàn bộ danh sách nguyên liệu thuần trong hệ thống (sắp xếp theo tên A-Z).
   * **`GET /api/ingredients/{id}`** *(Quyền: Public/User)*: Lấy thông tin chi tiết nguyên liệu theo ID.
   * **`GET /api/ingredients/{id}/aliases`** *(Quyền: Public/User)*: Lấy danh sách tên gọi khác (alias) của nguyên liệu.
   * **File liên quan**: [IngredientsController.cs](file:///d:/Proj/ZPantry-Backend/ZPantryModule/Controllers/IngredientsController.cs), [IngredientService.cs](file:///d:/Proj/ZPantry-Backend/ZPantryModule/Services/Implementations/IngredientService.cs), [ZPantryServiceInterfaces.cs](file:///d:/Proj/ZPantry-Backend/ZPantryModule/Services/Interfaces/ZPantryServiceInterfaces.cs).

---

### C. Cập Nhật Hạ Tầng & Docker Containerization

1. **Khôi phục cấu hình Git Submodule**:
   * Thiết lập file [.gitmodules](file:///d:/Proj/ZPantry-Backend/.gitmodules) liên kết dịch vụ `ZPantry-AIService` và đồng bộ mã nguồn về commit ổn định mới nhất.
2. **Khởi chạy Multi-container Environment**:
   * Đã rebuild thành công toàn bộ hệ thống bằng Docker Compose với các dịch vụ hoạt động ổn định:
     * 🐘 **PostgreSQL Database** (`zpantry-backend-postgres`): Port `5432`
     * 🤖 **AI Service** (`zpantry-ai-dev`): Port `8000`
     * 🌐 **Backend Core API** (`zpantry-backend-dev`): Port `8080` / `8081`

---

## 3. 🏁 Tổng Kết

Các thay đổi trên đã giúp hệ thống **ZPantry-Backend** đạt tiêu chuẩn cao hơn về:
* **Bảo mật & Phân quyền**: Ngăn chặn tối đa việc lạm dụng quyền Admin, đặt quyền sở hữu dữ liệu cá nhân về đúng tay người dùng.
* **Độ ổn định hệ thống**: Đồng bộ trơn tru 3 microservices/containers (Database, AI Service, API Backend) trong môi trường Docker, hỗ trợ hot-reload giúp quá trình kiểm thử và phát triển giao diện diễn ra nhanh chóng, chính xác.
