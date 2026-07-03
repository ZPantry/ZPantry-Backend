using Resend;
using Microsoft.Extensions.Options;

public class EmailSettings
{
    public string EmailAddress { get; set; } = string.Empty;

    public string DisplayName { get; set; } = "ZPantry";

    public string Password { get; set; } = string.Empty;

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; }

    public bool EnableSsl { get; set; }
}

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly IResend _resend;

    public EmailService(IOptions<EmailSettings> emailSettings)
    {
        _emailSettings = emailSettings.Value;
        ValidateSettings();
        _resend = ResendClient.Create(_emailSettings.Password);
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
    {
        var message = new EmailMessage
        {
            From = string.IsNullOrWhiteSpace(_emailSettings.DisplayName)
                ? _emailSettings.EmailAddress
                : $"{_emailSettings.DisplayName} <{_emailSettings.EmailAddress}>",
            Subject = subject,
            HtmlBody = htmlMessage
        };
        message.To.Add(toEmail);

        try
        {
            await _resend.EmailSendAsync(message);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Email delivery failed: {ex.Message}", ex);
        }
    }

    private void ValidateSettings()
    {
        if (string.IsNullOrWhiteSpace(_emailSettings.EmailAddress))
        {
            throw new InvalidOperationException("Gmail__EmailAddress is missing. Use your verified Resend from email here.");
        }

        if (string.IsNullOrWhiteSpace(_emailSettings.Password))
        {
            throw new InvalidOperationException("Gmail__Password is missing. Use your Resend API key here.");
        }
    }
}
