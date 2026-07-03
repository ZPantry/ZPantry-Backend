using System;
using System.Collections.Generic;
using System.Text;

using AuthenticationModule.Contracts.Common;
using AuthenticationModule.DTOs;

namespace AuthenticationModule.Services.Interfaces
{
    public interface IUserService
    {
        Task AddUser(RegisterRequest request);
        Task<bool> VerifyOtp(string email, string otpCode);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> GoogleLoginAsync(GoogleLoginRequest request);
        Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);
        Task LogoutAsync(string token);

        Task<PagedResponse<UserDto>> GetAllUsersAsync(int pageIndex, int pageSize);
        Task<ApiResponse<UserDto>> GetUserByIdAsync(Guid id);
        Task<ApiResponse<UserDto>> UpdateUserAsync(Guid id, UpdateUserRequest request);
        Task<ApiResponse<object>> DeleteUserAsync(Guid id);
    }
}
