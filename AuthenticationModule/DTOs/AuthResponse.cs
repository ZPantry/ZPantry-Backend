namespace AuthenticationModule.DTOs;

public class AuthResponse
{
    public Guid Id { get; set; }

    public string AccessToken { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }

    public string RefreshToken { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
}
