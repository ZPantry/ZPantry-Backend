using AuthenticationModule.Contracts.Common;
using AuthenticationModule.DTOs;

namespace ZPantryModule.Services.Interfaces;

public interface IIngredientService
{
    Task<PagedResponse<IngredientDto>> GetAllAsync(
        int pageIndex,
        int pageSize,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<IngredientDto>> CreateAsync(CreateIngredientRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<IngredientDto>> UpdateAsync(Guid id, UpdateIngredientRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IRecipeService
{
    Task<PagedResponse<RecipeDto>> GetAllAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default);
    Task<ApiResponse<RecipeDto>> CreateAsync(CreateRecipeRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<RecipeDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<RecipeDto>> UpdateAsync(Guid id, UpdateRecipeRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IUserPantryService
{
    Task<PagedResponse<PantryItemDto>> GetByUserIdAsync(Guid userId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
    Task<ApiResponse<PantryItemDto>> UpsertAsync(Guid userId, UpsertPantryItemRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<PantryItemDto>> UpdateAsync(Guid userId, Guid itemId, UpsertPantryItemRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> DeleteAsync(Guid userId, Guid itemId, CancellationToken cancellationToken = default);
}

public interface IRecommendationService
{
    Task<ApiResponse<RecommendMealResponse>> RecommendMealsAsync(Guid userId, RecommendMealRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<MissingIngredientSuggestionResponse>> SuggestMissingIngredientsAsync(Guid userId, RecommendMealRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> FeedbackAsync(Guid userId, Guid recommendationId, RecommendationFeedbackRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> GetByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);
}

public interface IAIRecommendationClient
{
    Task<ApiResponse<RecommendMealAiResponse>> RecommendMealsAsync(RecommendMealAiRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<MissingIngredientAiResponse>> SuggestMissingIngredientsAsync(MissingIngredientAiRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<EmbeddingAiResponse>> EmbedIngredientAsync(EmbedIngredientAiRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<EmbeddingAiResponse>> EmbedRecipeAsync(EmbedRecipeAiRequest request, CancellationToken cancellationToken = default);
}

public interface ICloudinaryStorageService
{
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
