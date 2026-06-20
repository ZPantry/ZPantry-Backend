namespace AuthenticationModule.DTOs;

public class JwtSettings
{
    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 60;

    public int RefreshTokenDays { get; set; } = 7;
}
