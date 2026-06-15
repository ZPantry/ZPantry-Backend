using System;
using System.Collections.Generic;
using System.Text;

namespace AuthenticationModule.Services.Interfaces
{
    public interface IUserService
    {
        Task AddUser(RegisterRequest request);
        Task<bool> VerifyOtp(string email, string otpCode);
    }
}
