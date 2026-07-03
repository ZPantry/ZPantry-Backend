using AuthenticationModule.Contracts.Common;
using AuthenticationModule.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ZPantryModule.Services.Interfaces;

namespace ZPantryModule.Controllers;

[ApiController]
[Authorize]
[Route("api/me/today-menu")]
public class TodayMenuController : ControllerBase
{
    private readonly ITodayMenuService _todayMenuService;

    public TodayMenuController(ITodayMenuService todayMenuService)
    {
        _todayMenuService = todayMenuService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<TodayMenuItemDto>>> GetByDate(
        [FromQuery] DateOnly? date = null,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(PagedResponse<TodayMenuItemDto>.FailPage("Invalid access token.", HttpContext.TraceIdentifier));
        }

        return Ok(await _todayMenuService.GetByUserAndDateAsync(userId.Value, date, pageIndex, pageSize));
    }

    [HttpGet("items/{id:guid}")]
    public async Task<ActionResult<ApiResponse<TodayMenuItemDetailDto>>> GetById(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ApiResponse<TodayMenuItemDetailDto>.Fail("Invalid access token.", traceId: HttpContext.TraceIdentifier));
        }

        return Ok(await _todayMenuService.GetByIdAsync(userId.Value, id));
    }

    [HttpPost("items")]
    public async Task<ActionResult<ApiResponse<TodayMenuItemDto>>> Create([FromBody] CreateTodayMenuItemRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ApiResponse<TodayMenuItemDto>.Fail("Invalid access token.", traceId: HttpContext.TraceIdentifier));
        }

        return Ok(await _todayMenuService.CreateAsync(userId.Value, request));
    }

    [HttpDelete("items/{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ApiResponse<object>.Fail("Invalid access token.", traceId: HttpContext.TraceIdentifier));
        }

        return Ok(await _todayMenuService.DeleteAsync(userId.Value, id));
    }

    [HttpPost("items/{id:guid}/complete")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<TodayMenuCompletionResponse>>> Complete(
        Guid id,
        [FromForm] CompleteTodayMenuItemRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ApiResponse<TodayMenuCompletionResponse>.Fail("Invalid access token.", traceId: HttpContext.TraceIdentifier));
        }

        return Ok(await _todayMenuService.CompleteAsync(userId.Value, id, request));
    }

    [HttpGet("/api/me/cooking-logs")]
    public async Task<ActionResult<PagedResponse<CookingLogDto>>> GetCookingLogs(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(PagedResponse<CookingLogDto>.FailPage("Invalid access token.", HttpContext.TraceIdentifier));
        }

        return Ok(await _todayMenuService.GetCookingLogsAsync(userId.Value, pageIndex, pageSize));
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirstValue("userId")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("nameid");

        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
