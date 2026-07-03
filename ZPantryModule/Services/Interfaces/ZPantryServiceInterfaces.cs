using AuthenticationModule.Contracts.Common;
using AuthenticationModule.DTOs;
using Microsoft.AspNetCore.Http;

namespace ZPantryModule.Services.Interfaces;

public interface IIngredientService
{
    Task<PagedResponse<IngredientDto>> GetAllAsync(
        int pageIndex,
        int pageSize,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<IngredientDto>> CreateAsync(CreateIngredientRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<IngredientDto>> CreateV2Async(ZPantryModule.DTOs.CreateIngredientFormRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<IngredientDto>> UpdateAsync(Guid id, UpdateIngredientRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<IngredientDto>> UpdateV2Async(Guid id, ZPantryModule.DTOs.UpdateIngredientFormRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IRecipeService
{
    Task<PagedResponse<RecipeDto>> GetAllAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default);
    Task<ApiResponse<RecipeDto>> CreateAsync(CreateRecipeRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<RecipeDto>> CreateV2Async(ZPantryModule.DTOs.CreateRecipeFormRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<RecipeDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<RecipeDto>> UpdateAsync(Guid id, UpdateRecipeRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<RecipeDto>> UpdateV2Async(Guid id, ZPantryModule.DTOs.UpdateRecipeFormRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IUserPantryService
{
    Task<PagedResponse<PantryItemDto>> GetByUserIdAsync(Guid userId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
    Task<ApiResponse<PantryItemDto>> UpsertAsync(Guid userId, UpsertPantryItemRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<PantryItemDto>> UpdateAsync(Guid userId, Guid itemId, UpdatePantryItemRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> DeleteAsync(Guid userId, Guid itemId, CancellationToken cancellationToken = default);
}

public interface IRecommendationService
{
    Task<ApiResponse<RecommendMealResponse>> RecommendMealsAsync(Guid userId, RecommendMealRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<MissingIngredientSuggestionResponse>> SuggestMissingIngredientsAsync(Guid userId, RecommendMealRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<MealIngredientCheckResponse>> CheckMealIngredientsAsync(Guid userId, Guid mealId, CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> FeedbackAsync(Guid userId, Guid recommendationId, RecommendationFeedbackRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> GetByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);
}

public interface IAIRecommendationClient
{
    Task<ApiResponse<RecommendMealAiResponse>> RecommendMealsAsync(RecommendMealAiRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<MissingIngredientAiResponse>> SuggestMissingIngredientsAsync(MissingIngredientAiRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<MealIngredientCheckAiResponse>> CheckMealIngredientsAsync(MealIngredientCheckAiRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<TodayMenuCompletionAiResponse>> CheckTodayMenuCompletionAsync(TodayMenuCompletionAiRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<EmbeddingAiResponse>> EmbedIngredientAsync(EmbedIngredientAiRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<EmbeddingAiResponse>> EmbedRecipeAsync(EmbedRecipeAiRequest request, CancellationToken cancellationToken = default);
}

public interface ICloudinaryStorageService
{
    Task<ApiResponse<MediaUploadResultDto>> UploadDetailedAsync(
        Stream fileStream,
        string fileName,
        Guid? ingredientId = null,
        Guid? recipeId = null,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<string>> UploadAsync(
        Stream fileStream,
        string fileName,
        Guid? ingredientId = null,
        Guid? recipeId = null,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<object>> DeleteAsync(string publicId, CancellationToken cancellationToken = default);
}

public interface IVectorSearchService
{
    Task<IReadOnlyList<object>> FindSimilarRecipesAsync(float[] embedding, int topK, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<object>> FindSimilarIngredientsAsync(float[] embedding, int topK, CancellationToken cancellationToken = default);
}

public interface ITodayMenuService
{
    Task<PagedResponse<TodayMenuItemDto>> GetByUserAndDateAsync(
        Guid userId,
        DateOnly? plannedDate,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<TodayMenuItemDetailDto>> GetByIdAsync(
        Guid userId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<TodayMenuItemDto>> CreateAsync(
        Guid userId,
        CreateTodayMenuItemRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<object>> DeleteAsync(
        Guid userId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<TodayMenuCompletionResponse>> CompleteAsync(
        Guid userId,
        Guid id,
        CompleteTodayMenuItemRequest request,
        CancellationToken cancellationToken = default);

    Task<PagedResponse<CookingLogDto>> GetCookingLogsAsync(
        Guid userId,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default);
}
