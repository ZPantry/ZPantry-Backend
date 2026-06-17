using AuthenticationModule.DTOs;
using AuthenticationModule.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthenticationModule.Controllers;

[ApiController]
[Route("api/[controller]")]
[ApiExplorerSettings(GroupName = "authentication")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;

    public AuthController(IUserService userService)
    {
        _userService = userService;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> RegisterNewUser([FromBody] RegisterRequest request)
    {
        try
        {
            await _userService.AddUser(request);
            return Ok(new { Message = "Đăng ký thành công! Vui lòng kiểm tra Gmail để nhận mã OTP xác thực." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyRequest request)
    {
        bool isVerified = await _userService.VerifyOtp(request.Email, request.OtpCode);
        if (!isVerified)
        {
            return BadRequest(new { Message = "Mã OTP không chính xác, đã hết hạn hoặc tài khoản đã được xác thực trước đó." });
        }

        return Ok(new { Message = "Xác thực tài khoản thành công! Bạn hiện đã có thể đăng nhập." });
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var result = await _userService.LoginAsync(request);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var result = await _userService.RefreshTokenAsync(request);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var authHeader = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Unauthorized(new { Message = "Missing bearer token." });
        }

        var token = authHeader["Bearer ".Length..].Trim();

        try
        {
            await _userService.LogoutAsync(token);
            return Ok(new { Message = "Logout successful." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = ex.Message });
        }
    }
}
