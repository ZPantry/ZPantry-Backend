using AuthenticationModule.Contracts.Common;
using AuthenticationModule.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ZPantryModule.Services.Interfaces;

namespace ZPantryModule.Controllers;

[ApiController]
[Authorize]
[Route("api/recommendations")]
public class RecommendationsController : ControllerBase
{
    private readonly IRecommendationService _recommendationService;

    public RecommendationsController(IRecommendationService recommendationService)
    {
        _recommendationService = recommendationService;
    }

    [HttpPost("meals")]
    public async Task<ActionResult<ApiResponse<RecommendMealResponse>>> RecommendMeals([FromBody] RecommendMealRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ApiResponse<RecommendMealResponse>.Fail("Invalid access token.", traceId: HttpContext.TraceIdentifier));
        }

        return Ok(await _recommendationService.RecommendMealsAsync(userId.Value, request));
    }

    [HttpPost("missing-ingredients")]
    public async Task<ActionResult<ApiResponse<MissingIngredientSuggestionResponse>>> SuggestMissingIngredients(
        [FromBody] RecommendMealRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ApiResponse<MissingIngredientSuggestionResponse>.Fail("Invalid access token.", traceId: HttpContext.TraceIdentifier));
        }

        return Ok(await _recommendationService.SuggestMissingIngredientsAsync(userId.Value, request));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> GetById(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ApiResponse<object>.Fail("Invalid access token.", traceId: HttpContext.TraceIdentifier));
        }

        return Ok(await _recommendationService.GetByIdAsync(userId.Value, id));
    }

    [HttpPost("{id:guid}/feedback")]
    public async Task<ActionResult<ApiResponse<object>>> Feedback(Guid id, [FromBody] RecommendationFeedbackRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized(ApiResponse<object>.Fail("Invalid access token.", traceId: HttpContext.TraceIdentifier));
        }

        return Ok(await _recommendationService.FeedbackAsync(userId.Value, id, request));
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirstValue("userId")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("nameid");

        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
