using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using Org.BouncyCastle.Security;

public class EmailSettings
{
    public string EmailAddress { get; set; } // Địa chỉ email gửi đi
    public string DisplayName { get; set; } // Tên hiển thị khi gửi email
    public string Password { get; set; }
    public string Host { get; set; } // SMTP server (ví dụ: smtp.gmail.com)
    public int Port { get; set; } // Port SMTP (ví dụ: 587 cho STARTTLS)
    public bool EnableSsl { get; set; }
}
public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;

    // Inject EmailSettings được map từ file authenticationconfig.json của bạn vào đây
    public EmailService(IOptions<EmailSettings> emailSettings)
    {
        _emailSettings = emailSettings.Value;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
    {
        var email = new MimeMessage();

        // Cấu hình người gửi (Lấy từ file json)
        email.From.Add(new MailboxAddress(_emailSettings.DisplayName, _emailSettings.EmailAddress));

        // Cấu hình người nhận
        email.To.Add(MailboxAddress.Parse(toEmail));

        // Tiêu đề thư
        email.Subject = subject;

        // Nội dung thư
        var builder = new BodyBuilder { HtmlBody = htmlMessage };
        
        // MẸO CHỐNG SPAM: Các bộ lọc thư rác (như Gmail) thường đánh dấu spam nếu email 
        // chỉ chứa HTML mà không có phiên bản Plain Text đi kèm. 
        // Ta dùng Regex để loại bỏ các thẻ HTML, tạo ra một bản Text thuần túy.
        builder.TextBody = System.Text.RegularExpressions.Regex.Replace(htmlMessage, "<.*?>", string.Empty).Trim();
        
        email.Body = builder.ToMessageBody();

        using var smtp = new SmtpClient();
        try
        {
            // Kết nối đến Server SMTP của Gmail (Port 587, bảo mật STARTTLS)
            await smtp.ConnectAsync(_emailSettings.Host, _emailSettings.Port, SecureSocketOptions.StartTls);

            // Đăng nhập bằng tài khoản và Mật khẩu ứng dụng đã cấu hình
            await smtp.AuthenticateAsync(_emailSettings.EmailAddress, _emailSettings.Password);

            // Thực hiện gửi
            await smtp.SendAsync(email);
        }
        catch (Exception ex)
        {
            // Nếu gửi lỗi, ném ra để tầng Service hoặc Controller biết để xử lý
            throw new Exception($"Lỗi hệ thống gửi mail: {ex.Message}");
        }
        finally
        {
            // Ngắt kết nối an toàn sau khi gửi xong
            await smtp.DisconnectAsync(true);
        }
    }
}