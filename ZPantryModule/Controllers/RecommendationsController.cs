using AuthenticationModule.Contracts.Common;
using AuthenticationModule.DTOs;
using Microsoft.AspNetCore.Mvc;
using ZPantryModule.Services.Interfaces;

namespace ZPantryModule.Controllers;

[ApiController]
[Route("api/recommendations")]
public class RecommendationsController : ControllerBase
{
    private readonly IRecommendationService _recommendationService;

    public RecommendationsController(IRecommendationService recommendationService)
    {
        _recommendationService = recommendationService;
    }

    [HttpPost("meals")]
    public Task<ApiResponse<RecommendMealResponse>> RecommendMeals([FromBody] RecommendMealRequest request)
        => _recommendationService.RecommendMealsAsync(request);

    [HttpPost("missing-ingredients")]
    public Task<ApiResponse<MissingIngredientSuggestionResponse>> SuggestMissingIngredients([FromBody] RecommendMealRequest request)
        => _recommendationService.SuggestMissingIngredientsAsync(request);

    [HttpGet("{id:guid}")]
    public Task<ApiResponse<object>> GetById(Guid id)
        => _recommendationService.GetByIdAsync(id);

    [HttpPost("{id:guid}/feedback")]
    public Task<ApiResponse<object>> Feedback(Guid id, [FromBody] RecommendationFeedbackRequest request)
        => _recommendationService.FeedbackAsync(id, request);
}

