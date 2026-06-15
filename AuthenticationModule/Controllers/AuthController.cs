using AuthenticationModule.DTOs;
using AuthenticationModule.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Http.Routing;
using static System.Net.WebRequestMethods;

namespace AuthenticationModule.Controllers
{
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
    }
}
