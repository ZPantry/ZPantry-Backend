using System;
using System.Collections.Generic;

namespace AuthenticationModule.Repositories.Entities;

public partial class User : BaseEntity
{
    public string? FullName { get; set; }

    public string Email { get; set; } = null!;

    public string? AvatarUrl { get; set; }

    public string? OtpCode { get; set; }

    public DateTime? OtpExpiredAt { get; set; }

    public int OtpRetryCount { get; set; }

    public bool IsEmailConfirmed { get; set; }

    public bool IsActive { get; set; }

    public string Role { get; set; } = "user";

    public string PasswordHashed { get; set; } = null!;

    public string? RefreshTokenHash { get; set; }

    public DateTime? RefreshTokenExpiresAt { get; set; }
}
