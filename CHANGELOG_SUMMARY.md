# 📋 Tóm Tắt Thay Đổi Mã Nguồn & Cập Nhật Hệ Thống (CHANGELOG)

---

## 1. 💡 Giới Thiệu

Tài liệu này tổng hợp toàn bộ các thay đổi mới nhất trong hệ thống **ZPantry-Backend**. Các cải tiến tập trung vào việc hoàn thiện tính năng CRUD cho các module cốt lõi (Người dùng & Nguyên liệu), điều chỉnh nghiệp vụ phân quyền bảo mật chặt chẽ theo vai trò thực tế, cùng với việc nâng cấp đồng bộ hạ tầng triển khai đa container trên Docker.

---

## 2. 🔄 Những Phần Thay Đổi (Nghiệp Vụ & API)

### A. Đăng Nhập Bằng Tài Khoản Google (Google OAuth Authentication)

> [!TIP]
> Hệ thống tích hợp đăng nhập nhanh qua tài khoản Google bên cạnh luồng đăng ký thủ công, hỗ trợ tự động khởi tạo tài khoản và xác thực email.

1. **Điều chỉnh Nghiệp vụ (Business Logic)**:
   * **Tự động cấp tài khoản (Auto-provisioning)**: Cho phép người dùng đăng nhập bằng Google ID Token (`idToken`). Nếu email chưa tồn tại trong hệ thống, tự động tạo mới tài khoản với trạng thái `IsEmailConfirmed = true` và đồng bộ `FullName`, `AvatarUrl` từ hồ sơ Google.
   * **Liên kết tài khoản hiện có**: Trường hợp email đã đăng ký thủ công trước đó, khi đăng nhập bằng Google thành công, hệ thống sẽ tự động xác thực email (nếu chưa xác thực) và bổ sung ảnh đại diện (nếu chưa có).
   * **Độ tin cậy & Fallback**: Xác thực chữ ký JWT ID Token bằng SDK chuẩn (`Google.Apis.Auth`) kết hợp cơ chế HTTP fallback tới Google API (`tokeninfo`/`userinfo`).

2. **Chi tiết API & Mã Nguồn**:
   * **`POST /api/auth/google-login`** *(Quyền: Public/AllowAnonymous)*: Nhận payload DTO `{ "idToken": "<JWT_OR_ACCESS_TOKEN>" }`, trả về cặp `AccessToken` và `RefreshToken` trong hệ thống ZPantry cùng thông tin người dùng (`Id`, `Email`, `FullName`, `AvatarUrl`, `Role`).
   * **File cấu hình & mã nguồn**: [AuthController.cs](file:///d:/Proj/ZPantry-Backend/AuthenticationModule/Controllers/AuthController.cs), [UserService.cs](file:///d:/Proj/ZPantry-Backend/AuthenticationModule/Services/Implementations/UserService.cs), [GoogleLoginRequest.cs](file:///d:/Proj/ZPantry-Backend/AuthenticationModule/DTOs/GoogleLoginRequest.cs), [GoogleSettings.cs](file:///d:/Proj/ZPantry-Backend/AuthenticationModule/DTOs/GoogleSettings.cs).

3. **Hướng Dẫn Cấu Hình & Kiểm Thử (Configuration & Testing Guide)**:
   * **Cấu hình Client ID**:
     * Khai báo trực tiếp trong file cấu hình [authenticationconfig.json](file:///d:/Proj/ZPantry-Backend/AuthenticationModule/authenticationconfig.json):
       ```json
       "Google": {
         "ClientId": "<YOUR_GOOGLE_CLIENT_ID>.apps.googleusercontent.com"
       }
       ```
     * Hoặc truyền qua biến môi trường Docker trong file [.env](file:///d:/Proj/ZPantry-Backend/.env):
       ```env
       Google__ClientId=<YOUR_GOOGLE_CLIENT_ID>.apps.googleusercontent.com
       ```
     * *(Lưu ý: Ngay cả khi tạm thời để trống `ClientId` lúc thử nghiệm ở môi trường dev, cơ chế xác thực vẫn linh hoạt hỗ trợ HTTP fallback giúp kiểm thử liền mạch mà không bị gián đoạn)*.
   * **Hướng dẫn test API qua Google OAuth 2.0 Playground**:
     1. Truy cập [Google OAuth 2.0 Playground](https://developers.google.com/oauthplayground/).
     2. Tại mục **Step 1**, dán chuỗi scopes sau vào ô *"Input your own scopes"* ở dưới cùng rồi bấm **Authorize APIs**:
        `https://www.googleapis.com/auth/userinfo.email https://www.googleapis.com/auth/userinfo.profile openid`
     3. Đăng nhập bằng tài khoản Gmail và nhấn **Continue / Đồng ý**.
     4. Tại mục **Step 2 (Exchange authorization code for tokens)**, bấm nút xanh dương **Exchange authorization code for tokens**.
     5. Copy chuỗi trong ô **`id_token`** (hoặc `Access token`), mở Swagger UI tại `http://localhost:8080/swagger`, gọi endpoint `POST /api/auth/google-login` với body `{ "idToken": "<chuỗi_token>" }` để xác thực.

---

### B. Quản Lý Người Dùng (User Management)

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
