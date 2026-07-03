using AuthenticationModule.Contracts.Common;
using AuthenticationModule.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ZPantryModule.Services.Interfaces;

namespace ZPantryModule.Controllers;

[ApiController]
[Authorize]
[Route("api/me/pantry")]
public class PantryController : ControllerBase
{
    private readonly IUserPantryService _userPantryService;

    public PantryController(IUserPantryService userPantryService)
    {
        _userPantryService = userPantryService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<PantryItemDto>>> GetByUserId(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(PagedResponse<PantryItemDto>.FailPage("Invalid access token.", HttpContext.TraceIdentifier));
        }

        return Ok(await _userPantryService.GetByUserIdAsync(userId.Value, pageIndex, pageSize));
    }

    [HttpGet("items")]
    public Task<ActionResult<PagedResponse<PantryItemDto>>> GetItems(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10)
        => GetByUserId(pageIndex, pageSize);

    [HttpPost("items")]
    public async Task<ActionResult<ApiResponse<PantryItemDto>>> Upsert([FromBody] UpsertPantryItemRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ApiResponse<PantryItemDto>.Fail("Invalid access token.", traceId: HttpContext.TraceIdentifier));
        }

        return Ok(await _userPantryService.UpsertAsync(userId.Value, request));
    }

    [HttpPut("items/{itemId:guid}")]
    public async Task<ActionResult<ApiResponse<PantryItemDto>>> Update(
        Guid itemId,
        [FromBody] UpdatePantryItemRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ApiResponse<PantryItemDto>.Fail("Invalid access token.", traceId: HttpContext.TraceIdentifier));
        }

        return Ok(await _userPantryService.UpdateAsync(userId.Value, itemId, request));
    }

    [HttpDelete("items/{itemId:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid itemId)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ApiResponse<object>.Fail("Invalid access token.", traceId: HttpContext.TraceIdentifier));
        }

        return Ok(await _userPantryService.DeleteAsync(userId.Value, itemId));
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirstValue("userId")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("nameid");

        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
