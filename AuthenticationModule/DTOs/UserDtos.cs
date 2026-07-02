namespace AuthenticationModule.DTOs;

public class UserDto
{
    public Guid Id { get; set; }

    public string? FullName { get; set; }

    public string Email { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }

    public bool IsEmailConfirmed { get; set; }

    public bool IsActive { get; set; }

    public string Role { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

public class UpdateUserRequest
{
    public string? FullName { get; set; }

    public string? AvatarUrl { get; set; }

    public string? Password { get; set; }
}
