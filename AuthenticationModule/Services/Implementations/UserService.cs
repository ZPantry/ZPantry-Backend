using AuthenticationModule.DTOs;
using AuthenticationModule.Repositories.Entities;
using AuthenticationModule.Repositories.Interfaces;
using AuthenticationModule.Services.Interfaces;
using Microsoft.AspNet.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AuthenticationModule.Services.Implementations;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly ITokenBlacklistService _tokenBlacklistService;
    private readonly JwtSettings _jwtSettings;

    public UserService(
        IUserRepository userRepository,
        IEmailService emailService,
        ITokenBlacklistService tokenBlacklistService,
        IOptions<JwtSettings> jwtSettings)
    {
        _userRepository = userRepository;
        _emailService = emailService;
        _tokenBlacklistService = tokenBlacklistService;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task AddUser(RegisterRequest request)
    {
        var existingUser = await _userRepository.GetUserByEmail(request.Email);
        if (existingUser != null)
        {
            throw new Exception("Email already exists.");
        }

        var generatedOtp = Random.Shared.Next(100000, 999999).ToString();
        var otpExpiry = DateTime.UtcNow.AddMinutes(5);

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            PasswordHashed = new PasswordHasher().HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null,
            OtpCode = generatedOtp,
            OtpExpiredAt = otpExpiry,
            OtpRetryCount = 0,
            IsEmailConfirmed = false,
            IsActive = true,
            Role = "user"
        };

        var subject = $"[{generatedOtp}] ZPantry account verification code";
        var htmlBody = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 5px;'>
                <h2 style='color: #4CAF50; text-align: center;'>ZPantry account verification</h2>
                <p>Hello <b>{user.FullName}</b>,</p>
                <p>Your OTP code is:</p>
                <div style='text-align: center; margin: 30px 0;'>
                    <span style='background-color: #f4f4f4; padding: 10px 20px; font-size: 24px; font-weight: bold; letter-spacing: 5px; border: 1px dashed #4CAF50; color: #333;'>
                        {generatedOtp}
                    </span>
                </div>
                <p style='color: #ff0000; font-size: 13px;'>* This code is valid for 5 minutes.</p>
            </div>";

        try
        {
            await _emailService.SendEmailAsync(user.Email, subject, htmlBody);
        }
        catch (Exception ex)
        {
            throw new Exception($"Could not send OTP email. Details: {ex.Message}");
        }

        await _userRepository.AddUser(user);
    }

    public async Task<bool> VerifyOtp(string email, string otpCode)
    {
        var user = await _userRepository.GetUserByEmail(email);
        if (user == null || user.IsEmailConfirmed)
        {
            return false;
        }

        if (user.OtpCode != otpCode || user.OtpExpiredAt == null || user.OtpExpiredAt < DateTime.UtcNow)
        {
            return false;
        }

        user.IsEmailConfirmed = true;
        user.OtpCode = null;
        user.OtpExpiredAt = null;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateUser(user);
        return true;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetUserByEmail(request.Email);
        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Account is inactive.");
        }

        if (!user.IsEmailConfirmed)
        {
            throw new UnauthorizedAccessException("Account is not verified yet.");
        }

        var verifyResult = new PasswordHasher().VerifyHashedPassword(user.PasswordHashed, request.Password);
        if (verifyResult == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        return await IssueTokensAsync(user, rotateRefreshToken: true);
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new UnauthorizedAccessException("Refresh token is required.");
        }

        var refreshTokenHash = HashRefreshToken(request.RefreshToken);
        var user = await _userRepository.GetUserByRefreshTokenHash(refreshTokenHash);
        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        if (!user.IsActive || !user.IsEmailConfirmed)
        {
            throw new UnauthorizedAccessException("Account is not allowed to refresh token.");
        }

        if (user.RefreshTokenExpiresAt == null || user.RefreshTokenExpiresAt <= DateTime.UtcNow)
        {
            user.RefreshTokenHash = null;
            user.RefreshTokenExpiresAt = null;
            await _userRepository.UpdateUser(user);
            throw new UnauthorizedAccessException("Refresh token has expired.");
        }

        return await IssueTokensAsync(user, rotateRefreshToken: true);
    }

    public async Task LogoutAsync(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            var jti = jwtToken.Claims.FirstOrDefault(claim => claim.Type == JwtRegisteredClaimNames.Jti)?.Value;
            var email = jwtToken.Claims.FirstOrDefault(claim => claim.Type == JwtRegisteredClaimNames.Email)?.Value
                ?? jwtToken.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.Email)?.Value
                ?? jwtToken.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.Name)?.Value;

            if (string.IsNullOrWhiteSpace(jti) || string.IsNullOrWhiteSpace(email))
            {
                throw new UnauthorizedAccessException("Token is invalid.");
            }

            var user = await _userRepository.GetUserByEmail(email);
            if (user != null)
            {
                user.RefreshTokenHash = null;
                user.RefreshTokenExpiresAt = null;
                user.UpdatedAt = DateTime.UtcNow;
                await _userRepository.UpdateUser(user);
            }

            var expiresAt = jwtToken.ValidTo == DateTime.MinValue
                ? DateTimeOffset.UtcNow.AddMinutes(_jwtSettings.AccessTokenMinutes)
                : new DateTimeOffset(DateTime.SpecifyKind(jwtToken.ValidTo, DateTimeKind.Utc));

            await _tokenBlacklistService.RevokeAsync(jti, expiresAt);
        }
        catch (Exception ex) when (ex is not UnauthorizedAccessException)
        {
            throw new UnauthorizedAccessException("Invalid token.");
        }
    }

    private async Task<AuthResponse> IssueTokensAsync(User user, bool rotateRefreshToken)
    {
        var accessToken = CreateAccessToken(user);

        string refreshToken;
        if (rotateRefreshToken || string.IsNullOrWhiteSpace(user.RefreshTokenHash))
        {
            refreshToken = GenerateRefreshToken();
            user.RefreshTokenHash = HashRefreshToken(refreshToken);
            user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays);
        }
        else
        {
            refreshToken = string.Empty;
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateUser(user);

        return new AuthResponse
        {
            AccessToken = accessToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenMinutes),
            FullName = user.FullName ?? string.Empty,
            Email = user.Email,
            RefreshToken = refreshToken,
            Role = user.Role
        };
    }

    private string CreateAccessToken(User user)
    {
        if (string.IsNullOrWhiteSpace(_jwtSettings.SecretKey))
        {
            throw new InvalidOperationException("JWT SecretKey is missing.");
        }

        if (Encoding.UTF8.GetByteCount(_jwtSettings.SecretKey) < 32)
        {
            throw new InvalidOperationException("JWT SecretKey must be at least 32 bytes for HS256.");
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenMinutes);
        var jti = Guid.NewGuid().ToString("N");
        var role = string.IsNullOrWhiteSpace(user.Role) ? "user" : user.Role.Trim().ToLowerInvariant();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Email),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, jti),
            new("userId", user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName ?? string.Empty),
            new("fullName", user.FullName ?? string.Empty),
            new("isEmailConfirmed", user.IsEmailConfirmed.ToString().ToLowerInvariant()),
            new(ClaimTypes.Role, role),
            new("role", role)
        };

        var token = new JwtSecurityToken(
            issuer: string.IsNullOrWhiteSpace(_jwtSettings.Issuer) ? null : _jwtSettings.Issuer,
            audience: string.IsNullOrWhiteSpace(_jwtSettings.Audience) ? null : _jwtSettings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    private static string HashRefreshToken(string refreshToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToHexString(bytes);
    }
}
