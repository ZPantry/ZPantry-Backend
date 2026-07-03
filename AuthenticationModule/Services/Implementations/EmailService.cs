using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

public class EmailSettings
{
    public string EmailAddress { get; set; } = string.Empty;

    public string DisplayName { get; set; } = "ZPantry";

    public string Password { get; set; } = string.Empty;

    public string Host { get; set; } = "smtp.gmail.com";

    public int Port { get; set; } = 587;

    public bool EnableSsl { get; set; } = true;
}

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;

    public EmailService(IOptions<EmailSettings> emailSettings)
    {
        _emailSettings = emailSettings.Value;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
    {
        ValidateSettings();

        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(_emailSettings.DisplayName, _emailSettings.EmailAddress));
        email.To.Add(MailboxAddress.Parse(toEmail));
        email.Subject = subject;

        var builder = new BodyBuilder
        {
            HtmlBody = htmlMessage,
            TextBody = System.Text.RegularExpressions.Regex.Replace(htmlMessage, "<.*?>", string.Empty).Trim()
        };
        email.Body = builder.ToMessageBody();

        using var smtp = new SmtpClient();

        try
        {
            var socketOptions = _emailSettings.EnableSsl
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.Auto;

            await smtp.ConnectAsync(_emailSettings.Host, _emailSettings.Port, socketOptions);

            smtp.AuthenticationMechanisms.Remove("XOAUTH2");
            smtp.AuthenticationMechanisms.Remove("NTLM");
            smtp.AuthenticationMechanisms.Remove("GSSAPI");

            await smtp.AuthenticateAsync(_emailSettings.EmailAddress, _emailSettings.Password);
            await smtp.SendAsync(email);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Email delivery failed: {ex.Message}", ex);
        }
        finally
        {
            if (smtp.IsConnected)
            {
                await smtp.DisconnectAsync(true);
            }
        }
    }

    private void ValidateSettings()
    {
        if (string.IsNullOrWhiteSpace(_emailSettings.EmailAddress))
        {
            throw new InvalidOperationException("Gmail__EmailAddress is missing.");
        }

        if (string.IsNullOrWhiteSpace(_emailSettings.Password))
        {
            throw new InvalidOperationException("Gmail__Password is missing. Use a Google App Password, not the normal Gmail password.");
        }

        if (string.IsNullOrWhiteSpace(_emailSettings.Host))
        {
            throw new InvalidOperationException("Gmail__Host is missing.");
        }

        if (_emailSettings.Port <= 0)
        {
            throw new InvalidOperationException("Gmail__Port is invalid.");
        }
    }
}
