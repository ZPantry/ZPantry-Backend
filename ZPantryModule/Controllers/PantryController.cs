using AuthenticationModule.Contracts.Common;
using AuthenticationModule.DTOs;
using Microsoft.AspNetCore.Mvc;
using ZPantryModule.Services.Interfaces;

namespace ZPantryModule.Controllers;

[ApiController]
[Route("api/users/{userId:guid}/pantry")]
public class PantryController : ControllerBase
{
    private readonly IUserPantryService _userPantryService;

    public PantryController(IUserPantryService userPantryService)
    {
        _userPantryService = userPantryService;
    }

    [HttpGet]
    public Task<PagedResponse<PantryItemDto>> GetByUserId(Guid userId, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        => _userPantryService.GetByUserIdAsync(userId, pageIndex, pageSize);

    [HttpPost("items")]
    public Task<ApiResponse<PantryItemDto>> Upsert(Guid userId, [FromBody] UpsertPantryItemRequest request)
        => _userPantryService.UpsertAsync(userId, request);

    [HttpPut("items/{itemId:guid}")]
    public Task<ApiResponse<PantryItemDto>> Update(Guid userId, Guid itemId, [FromBody] UpdatePantryItemRequest request)
        => _userPantryService.UpdateAsync(userId, itemId, request);

    [HttpDelete("items/{itemId:guid}")]
    public Task<ApiResponse<object>> Delete(Guid userId, Guid itemId)
        => _userPantryService.DeleteAsync(userId, itemId);
}

