using AuthenticationModule.Repositories.Entities;
using AuthenticationModule.Repositories.Interfaces;
using AuthenticationModule.Services.Interfaces;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace AuthenticationModule.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;
        public UserService(IUserRepository userRepository, IEmailService emailService)
        {
            _userRepository = userRepository;
            _emailService = emailService;
        }

        public async Task AddUser(RegisterRequest request)
        {
            // 1. Kiểm tra xem email đã được đăng ký chưa
            var existingUser = await _userRepository.GetUserByEmail(request.Email);
            if (existingUser != null)
            {
                throw new Exception("Địa chỉ email này đã được sử dụng.");
            }

            // 2. Sinh mã OTP ngẫu nhiên gồm 6 chữ số
            string generatedOtp = new Random().Next(100000, 999999).ToString();

            // 2. Thiết lập thời gian hết hạn cho OTP (Ví dụ: 5 phút tính từ hiện tại)
            DateTime otpExpiry = DateTime.Now.AddMinutes(5);
            
            var user = new User{
                FullName = request.FullName,
                Email = request.Email,
                PasswordHashed = new PasswordHasher().HashPassword(request.Password),
                CreatedAt = DateTime.Now,
                UpdatedAt = null,
                OtpCode = generatedOtp,
                OtpExpiredAt = otpExpiry,
                OtpRetryCount = 0,
                IsEmailConfirmed = false,
                IsActive = true
            };
            await _userRepository.AddUser(user);

            // Tiêu đề cụ thể và rõ ràng hơn để tránh bộ lọc Spam
            string subject = $"[{generatedOtp}] Mã Xác Thực Tài Khoản ZPantry Của Bạn";
            
            // Bổ sung đoạn Footer giải thích lý do nhận mail (Rất quan trọng để bypass Spam filter)
            string htmlBody = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 5px;'>
                <h2 style='color: #4CAF50; text-align: center;'>Xác Thực Tài Khoản ZPantry</h2>
                <p>Chào bạn <b>{user.FullName}</b>,</p>
                <p>Cảm ơn bạn đã đăng ký thành viên tại ZPantry. Mã OTP để kích hoạt tài khoản của bạn là:</p>
                <div style='text-align: center; margin: 30px 0;'>
                    <span style='background-color: #f4f4f4; padding: 10px 20px; font-size: 24px; font-weight: bold; letter-spacing: 5px; border: 1px dashed #4CAF50; color: #333;'>
                        {generatedOtp}
                    </span>
                </div>
                <p style='color: #ff0000; font-size: 13px;'>* Mã OTP này có hiệu lực trong vòng 5 phút. Vui lòng không chia sẻ mã này cho bất kỳ ai.</p>
                <br/>
                <p>Trân trọng,<br/><b>Đội ngũ ZPantry</b></p>
                <hr style='border: none; border-top: 1px solid #eee; margin-top: 20px;' />
                <div style='color: #888; font-size: 11px; text-align: center;'>
                    <p>Bạn nhận được email này vì địa chỉ email của bạn đã được sử dụng để đăng ký tài khoản tại hệ thống ZPantry.</p>
                    <p>Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email này. Tài khoản sẽ không được kích hoạt.</p>
                </div>
            </div>";
            try
            {
                await _emailService.SendEmailAsync(user.Email, subject, htmlBody);
            }
            catch (Exception ex)
            {
                // Nếu gửi mail bị lỗi, tiến hành xóa bản ghi User vừa tạo để rollback dữ liệu
                await _userRepository.DeleteUser(user);
                throw new Exception($"Không thể gửi email OTP. Vui lòng kiểm tra lại địa chỉ email hoặc thử lại sau. Lỗi chi tiết: {ex.Message}");
            }
        }

        public async Task<bool> VerifyOtp(string email, string otpCode)
        {
            // 1. Tìm kiếm User trong Database dựa vào Email
            // (Bạn lưu ý kiểm tra xem tầng Repository của bạn đã có hàm tương tự như GetUserByEmail chưa nhé)
            var user = await _userRepository.GetUserByEmail(email);

            // Nếu không tìm thấy User, trả về false ngay lập tức
            if (user == null) return false;

            // 2. Kiểm tra xem tài khoản này đã được xác thực từ trước chưa (để tránh xử lý thừa)
            if (user.IsEmailConfirmed) return false;

            // 3. Kiểm tra xem mã OTP người dùng nhập vào có khớp với mã lưu trong DB không
            if (user.OtpCode != otpCode)
            {
                // Bạn có thể mở rộng thêm logic đếm số lần nhập sai ở đây nếu cần: user.OtpRetryCount++;
                return false;
            }

            // 4. Kiểm tra xem mã OTP đã bị hết hạn chưa (quá 5 phút kể từ lúc sinh mã)
            if (user.OtpExpiredAt < DateTime.Now)
            {
                return false; // OTP đã hết hạn
            }

            // 5. Nếu vượt qua tất cả các điều kiện trên -> Mã OTP hoàn toàn hợp lệ!
            user.IsEmailConfirmed = true;   // Kích hoạt trạng thái đã xác thực
            user.OtpCode = null;            // Xóa mã OTP cũ đi để bảo mật
            user.OtpExpiredAt = null;       // Xóa thời gian hết hạn của OTP cũ
            user.UpdatedAt = DateTime.Now;

            // 6. Cập nhật lại thông tin User đã thay đổi xuống Database
            // (Bạn kiểm tra xem Repository đã có hàm UpdateUser chưa nha)
            await _userRepository.UpdateUser(user);

            return true; // Xác thực thành công!
        }
    }
}
