using System;
using System.Collections.Generic;

namespace AuthenticationModule.Repositories.Entities;

public partial class User
{
    public Guid Id { get; set; }

    public string? FullName { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHashed { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? OtpCode { get; set; }

    public DateTime? OtpExpiredAt { get; set; }

    public int OtpRetryCount { get; set; }

    public bool IsEmailConfirmed { get; set; }

    public bool IsActive { get; set; }
}
