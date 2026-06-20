using System;
using System.Collections.Generic;
using System.Text;

using AuthenticationModule.DTOs;

namespace AuthenticationModule.Services.Interfaces
{
    public interface IUserService
    {
        Task AddUser(RegisterRequest request);
        Task<bool> VerifyOtp(string email, string otpCode);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);
        Task LogoutAsync(string token);
    }
}
