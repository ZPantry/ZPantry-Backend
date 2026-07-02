using System.Security.Claims;
using AuthenticationModule.Contracts.Common;
using AuthenticationModule.DTOs;
using AuthenticationModule.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthenticationModule.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [Authorize(Roles = "admin")]
    [HttpGet]
    public Task<PagedResponse<UserDto>> GetAll([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        => _userService.GetAllUsersAsync(pageIndex, pageSize);

    [Authorize(Roles = "admin")]
    [HttpGet("{id:guid}")]
    public Task<ApiResponse<UserDto>> GetById(Guid id)
        => _userService.GetUserByIdAsync(id);

    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Update(Guid id, [FromBody] UpdateUserRequest request)
    {
        var currentUserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(currentUserIdClaim, out var currentUserId) || currentUserId != id)
        {
            return StatusCode(403, ApiResponse<UserDto>.Fail("Bạn chỉ được phép cập nhật thông tin của chính tài khoản mình.", traceId: HttpContext.TraceIdentifier));
        }

        var result = await _userService.UpdateUserAsync(id, request);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [Authorize(Roles = "admin")]
    [HttpDelete("{id:guid}")]
    public Task<ApiResponse<object>> Delete(Guid id)
        => _userService.DeleteUserAsync(id);
}
