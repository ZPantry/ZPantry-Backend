namespace AuthenticationModule.DTOs;

public class VerifyRequest
{
    public string OtpCode { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}
