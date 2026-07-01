using AuthenticationModule.Contracts.Common;
using AuthenticationModule.DTOs;
using AuthenticationModule.Repositories.Entities;
using Microsoft.EntityFrameworkCore;
using ZPantryModule.Services.Interfaces;

namespace ZPantryModule.Services.Implementations;

public class RecommendationService : IRecommendationService
{
    private readonly ZpantryDbContext _dbContext;
    private readonly IAIRecommendationClient _aiRecommendationClient;

    public RecommendationService(ZpantryDbContext dbContext, IAIRecommendationClient aiRecommendationClient)
    {
        _dbContext = dbContext;
        _aiRecommendationClient = aiRecommendationClient;
    }

    public async Task<ApiResponse<RecommendMealResponse>> RecommendMealsAsync(
        Guid userId,
        RecommendMealRequest request,
        CancellationToken cancellationToken = default)
    {
        var userIngredients = await GetUserIngredientItemsAsync(userId, request, cancellationToken);
        var candidateRecipes = await GetCandidateRecipesAsync(request.CandidateRecipes, cancellationToken);

        var recommendation = new MealRecommendation
        {
            UserId = userId,
            RequestText = string.Join(", ", request.CandidateRecipes.Concat(request.Ingredients)),
            InputIngredientText = request.InputIngredientText,
            RecommendationType = "meal",
            Status = "processing"
        };

        _dbContext.MealRecommendations.Add(recommendation);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var aiResponse = await _aiRecommendationClient.RecommendMealsAsync(
            new RecommendMealAiRequest
            {
                UserId = userId,
                InputIngredientText = request.InputIngredientText,
                Ingredients = userIngredients,
                CandidateRecipes = candidateRecipes,
                TopK = request.TopK <= 0 ? 5 : request.TopK
            },
            cancellationToken);

        if (!aiResponse.Success || aiResponse.Data is null)
        {
            recommendation.Status = "failed";
            recommendation.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return ApiResponse<RecommendMealResponse>.Fail(aiResponse.Message);
        }

        recommendation.Status = "completed";
        recommendation.CompletedAt = DateTime.UtcNow;
        recommendation.UpdatedAt = DateTime.UtcNow;

        foreach (var item in aiResponse.Data.Items)
        {
            _dbContext.MealRecommendationItems.Add(new MealRecommendationItem
            {
                MealRecommendationId = recommendation.Id,
                RecipeId = item.RecipeId,
                MatchScore = item.MatchScore,
                MissingIngredientCount = item.MissingIngredientCount,
                MissingIngredientNames = string.Join(", ", item.MissingIngredientNames),
                Reason = item.Reason,
                Rank = item.Rank
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new RecommendMealResponse
        {
            Items = aiResponse.Data.Items
                .Select(item => new RecommendMealResponseItem
                {
                    RecipeId = item.RecipeId,
                    RecipeName = item.RecipeName,
                    MatchScore = item.MatchScore,
                    MissingIngredientCount = item.MissingIngredientCount,
                    MissingIngredientNames = item.MissingIngredientNames,
                    Reason = item.Reason,
                    Rank = item.Rank
                })
                .ToList()
        };

        return ApiResponse<RecommendMealResponse>.SuccessResponse(response, "Meal recommendations generated.");
    }

    public async Task<ApiResponse<MissingIngredientSuggestionResponse>> SuggestMissingIngredientsAsync(
        Guid userId,
        RecommendMealRequest request,
        CancellationToken cancellationToken = default)
    {
        var recipe = await FindRequestedRecipeAsync(request.CandidateRecipes, cancellationToken);
        if (recipe is null)
        {
            return ApiResponse<MissingIngredientSuggestionResponse>.Fail("Recipe not found for missing ingredient suggestion.");
        }

        var requiredIngredients = await GetRecipeIngredientNamesAsync(recipe.Id, cancellationToken);
        var userIngredients = (await GetUserIngredientItemsAsync(userId, request, cancellationToken))
            .Select(item => item.Name)
            .ToList();

        var aiResponse = await _aiRecommendationClient.SuggestMissingIngredientsAsync(
            new MissingIngredientAiRequest
            {
                RecipeId = recipe.Id,
                RecipeName = recipe.Name,
                RequiredIngredients = requiredIngredients,
                UserIngredients = userIngredients
            },
            cancellationToken);

        if (!aiResponse.Success || aiResponse.Data is null)
        {
            return ApiResponse<MissingIngredientSuggestionResponse>.Fail(aiResponse.Message);
        }

        return ApiResponse<MissingIngredientSuggestionResponse>.SuccessResponse(
            new MissingIngredientSuggestionResponse
            {
                RecipeId = aiResponse.Data.RecipeId,
                MissingIngredients = aiResponse.Data.MissingIngredients
            },
            "Missing ingredients suggested.");
    }

    public async Task<ApiResponse<object>> FeedbackAsync(
        Guid userId,
        Guid recommendationId,
        RecommendationFeedbackRequest request,
        CancellationToken cancellationToken = default)
    {
        var recommendationExists = await _dbContext.MealRecommendations.AnyAsync(
            recommendation => recommendation.Id == recommendationId
                && recommendation.UserId == userId
                && !recommendation.IsDeleted,
            cancellationToken);

        if (!recommendationExists)
        {
            return ApiResponse<object>.Fail("Recommendation not found.");
        }

        var feedback = new RecommendationFeedback
        {
            UserId = userId,
            MealRecommendationId = recommendationId,
            RecipeId = request.RecipeId,
            Rating = request.Rating,
            FeedbackType = request.FeedbackType,
            Comment = request.Comment
        };

        _dbContext.RecommendationFeedbacks.Add(feedback);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponse<object>.SuccessResponse(new { feedback.Id }, "Feedback saved.");
    }

    public async Task<ApiResponse<object>> GetByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken = default)
    {
        var recommendation = await _dbContext.MealRecommendations
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == id
                    && item.UserId == userId
                    && !item.IsDeleted,
                cancellationToken);

        if (recommendation is null)
        {
            return ApiResponse<object>.Fail("Recommendation not found.");
        }

        var recommendationItems = await _dbContext.MealRecommendationItems
            .AsNoTracking()
            .Where(item => item.MealRecommendationId == id && !item.IsDeleted)
            .OrderBy(item => item.Rank)
            .ToListAsync(cancellationToken);

        return ApiResponse<object>.SuccessResponse(new
        {
            recommendation.Id,
            recommendation.InputIngredientText,
            recommendation.RecommendationType,
            recommendation.Status,
            recommendation.CompletedAt,
            Items = recommendationItems.Select(item => new
            {
                item.Id,
                item.RecipeId,
                item.MatchScore,
                item.MissingIngredientCount,
                MissingIngredientNames = SplitNames(item.MissingIngredientNames),
                item.Reason,
                item.Rank
            })
        });
    }

    private async Task<List<AiIngredientItem>> GetUserIngredientItemsAsync(
        Guid userId,
        RecommendMealRequest request,
        CancellationToken cancellationToken)
    {
        var pantryIngredients = await (
                from pantryItem in _dbContext.UserPantryItems.AsNoTracking()
                join ingredient in _dbContext.Ingredients.AsNoTracking()
                    on pantryItem.IngredientId equals ingredient.Id
                where pantryItem.UserId == userId
                    && !pantryItem.IsDeleted
                    && !ingredient.IsDeleted
                select new AiIngredientItem
                {
                    IngredientId = ingredient.Id,
                    Name = ingredient.Name,
                    Quantity = pantryItem.Quantity,
                    Unit = pantryItem.Unit
                })
            .ToListAsync(cancellationToken);

        var selectedIds = request.SelectedIngredients
            .Where(item => item.IngredientId != Guid.Empty)
            .Select(item => item.IngredientId)
            .Distinct()
            .ToList();

        var ingredientNamesById = selectedIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _dbContext.Ingredients
                .AsNoTracking()
                .Where(ingredient => selectedIds.Contains(ingredient.Id) && !ingredient.IsDeleted)
                .ToDictionaryAsync(ingredient => ingredient.Id, ingredient => ingredient.Name, cancellationToken);

        var selectedIngredients = request.SelectedIngredients
            .Select(item =>
            {
                ingredientNamesById.TryGetValue(item.IngredientId, out var ingredientName);
                return new AiIngredientItem
                {
                    IngredientId = item.IngredientId == Guid.Empty ? null : item.IngredientId,
                    Name = ingredientName ?? item.Name ?? string.Empty,
                    Quantity = item.Quantity,
                    Unit = item.Unit
                };
            });

        var typedIngredients = request.Ingredients
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => new AiIngredientItem
            {
                Name = name.Trim()
            });

        return pantryIngredients
            .Concat(selectedIngredients)
            .Concat(typedIngredients)
            .Where(item => !string.IsNullOrWhiteSpace(item.Name))
            .GroupBy(item => item.IngredientId?.ToString() ?? item.Name.Trim().ToLowerInvariant())
            .Select(group => group.First())
            .ToList();
    }

    private async Task<List<AiCandidateRecipeItem>> GetCandidateRecipesAsync(
        IReadOnlyCollection<string> requestedCandidates,
        CancellationToken cancellationToken)
    {
        var recipes = await _dbContext.Recipes
            .AsNoTracking()
            .Where(recipe => !recipe.IsDeleted)
            .ToListAsync(cancellationToken);

        if (requestedCandidates.Count > 0)
        {
            recipes = recipes
                .Where(recipe => requestedCandidates.Any(candidate =>
                    Guid.TryParse(candidate, out var candidateId)
                        ? recipe.Id == candidateId
                        : string.Equals(recipe.Name, candidate, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        var result = new List<AiCandidateRecipeItem>();
        foreach (var recipe in recipes)
        {
            result.Add(new AiCandidateRecipeItem
            {
                RecipeId = recipe.Id,
                RecipeName = recipe.Name,
                IngredientNames = await GetRecipeIngredientNamesAsync(recipe.Id, cancellationToken),
                InstructionText = recipe.InstructionText
            });
        }

        return result;
    }

    private async Task<Recipe?> FindRequestedRecipeAsync(
        IReadOnlyCollection<string> requestedCandidates,
        CancellationToken cancellationToken)
    {
        var recipes = await _dbContext.Recipes
            .Where(recipe => !recipe.IsDeleted)
            .OrderBy(recipe => recipe.Name)
            .ToListAsync(cancellationToken);

        if (requestedCandidates.Count == 0)
        {
            return recipes.FirstOrDefault();
        }

        return recipes.FirstOrDefault(recipe => requestedCandidates.Any(candidate =>
            Guid.TryParse(candidate, out var candidateId)
                ? recipe.Id == candidateId
                : string.Equals(recipe.Name, candidate, StringComparison.OrdinalIgnoreCase)));
    }

    private async Task<List<string>> GetRecipeIngredientNamesAsync(
        Guid recipeId,
        CancellationToken cancellationToken)
        => await (
                from recipeIngredient in _dbContext.RecipeIngredients.AsNoTracking()
                join ingredient in _dbContext.Ingredients.AsNoTracking()
                    on recipeIngredient.IngredientId equals ingredient.Id
                where recipeIngredient.RecipeId == recipeId
                    && !recipeIngredient.IsDeleted
                    && !ingredient.IsDeleted
                select ingredient.Name)
            .Distinct()
            .ToListAsync(cancellationToken);

    private static List<string> SplitNames(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}
