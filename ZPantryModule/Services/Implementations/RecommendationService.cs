using AuthenticationModule.Contracts.Common;
using AuthenticationModule.DTOs;
using ZPantryModule.Services.Interfaces;

namespace ZPantryModule.Services.Implementations;

public class RecommendationService : IRecommendationService
{
    public Task<ApiResponse<RecommendMealResponse>> RecommendMealsAsync(RecommendMealRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(ApiResponse<RecommendMealResponse>.Fail("Recommendation service not implemented yet."));

    public Task<ApiResponse<MissingIngredientSuggestionResponse>> SuggestMissingIngredientsAsync(RecommendMealRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(ApiResponse<MissingIngredientSuggestionResponse>.Fail("Recommendation service not implemented yet."));

    public Task<ApiResponse<object>> FeedbackAsync(Guid recommendationId, RecommendationFeedbackRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(ApiResponse<object>.Fail("Recommendation service not implemented yet."));

    public Task<ApiResponse<object>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(ApiResponse<object>.Fail("Recommendation service not implemented yet."));
}

