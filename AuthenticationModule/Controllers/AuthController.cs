using AuthenticationModule.Contracts.Common;
using AuthenticationModule.DTOs;
using AuthenticationModule.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthenticationModule.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;

    public AuthController(IUserService userService)
    {
        _userService = userService;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<object>>> RegisterNewUser([FromBody] RegisterRequest request)
    {
        try
        {
            await _userService.AddUser(request);
            return Ok(ApiResponse<object>.SuccessResponse(
                null,
                "Đăng ký thành công! Vui lòng kiểm tra Gmail để nhận mã OTP xác thực.",
                HttpContext.TraceIdentifier));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.Fail(ex.Message, traceId: HttpContext.TraceIdentifier));
        }
    }

    [AllowAnonymous]
    [HttpPost("verify-otp")]
    public async Task<ActionResult<ApiResponse<object>>> VerifyOtp([FromBody] VerifyRequest request)
    {
        var isVerified = await _userService.VerifyOtp(request.Email, request.OtpCode);
        if (!isVerified)
        {
            return BadRequest(ApiResponse<object>.Fail(
                "Mã OTP không chính xác, đã hết hạn hoặc tài khoản đã được xác thực trước đó.",
                traceId: HttpContext.TraceIdentifier));
        }

        return Ok(ApiResponse<object>.SuccessResponse(
            null,
            "Xác thực tài khoản thành công! Bạn hiện đã có thể đăng nhập.",
            HttpContext.TraceIdentifier));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var result = await _userService.LoginAsync(request);
            return Ok(ApiResponse<AuthResponse>.SuccessResponse(result, traceId: HttpContext.TraceIdentifier));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<AuthResponse>.Fail(ex.Message, traceId: HttpContext.TraceIdentifier));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<AuthResponse>.Fail(ex.Message, traceId: HttpContext.TraceIdentifier));
        }
    }

    [AllowAnonymous]
    [HttpPost("google-login")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        try
        {
            var result = await _userService.GoogleLoginAsync(request);
            return Ok(ApiResponse<AuthResponse>.SuccessResponse(result, "Đăng nhập Google thành công!", traceId: HttpContext.TraceIdentifier));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<AuthResponse>.Fail(ex.Message, traceId: HttpContext.TraceIdentifier));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<AuthResponse>.Fail(ex.Message, traceId: HttpContext.TraceIdentifier));
        }
    }

    [AllowAnonymous]
    [HttpPost("refresh-token")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var result = await _userService.RefreshTokenAsync(request);
            return Ok(ApiResponse<AuthResponse>.SuccessResponse(result, traceId: HttpContext.TraceIdentifier));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<AuthResponse>.Fail(ex.Message, traceId: HttpContext.TraceIdentifier));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<AuthResponse>.Fail(ex.Message, traceId: HttpContext.TraceIdentifier));
        }
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<object>>> Logout()
    {
        var authHeader = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Unauthorized(ApiResponse<object>.Fail("Missing bearer token.", traceId: HttpContext.TraceIdentifier));
        }

        var token = authHeader["Bearer ".Length..].Trim();

        try
        {
            await _userService.LogoutAsync(token);
            return Ok(ApiResponse<object>.SuccessResponse(null, "Logout successful.", HttpContext.TraceIdentifier));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<object>.Fail(ex.Message, traceId: HttpContext.TraceIdentifier));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.Fail(ex.Message, traceId: HttpContext.TraceIdentifier));
        }
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<ActionResult<ApiResponse<object>>> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        try
        {
            await _userService.ForgotPasswordAsync(request);
            return Ok(ApiResponse<object>.SuccessResponse(null, "Mã OTP đặt lại mật khẩu đã được gửi đến email của bạn.", HttpContext.TraceIdentifier));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<object>.Fail(ex.Message, traceId: HttpContext.TraceIdentifier));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, traceId: HttpContext.TraceIdentifier));
        }
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        try
        {
            await _userService.ResetPasswordAsync(request);
            return Ok(ApiResponse<object>.SuccessResponse(null, "Đặt lại mật khẩu thành công! Bạn có thể đăng nhập bằng mật khẩu mới.", HttpContext.TraceIdentifier));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, traceId: HttpContext.TraceIdentifier));
        }
    }
}
