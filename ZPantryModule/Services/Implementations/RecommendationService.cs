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

        RecommendMealResponse response;
        string message;

        if (aiResponse.Success && aiResponse.Data is not null)
        {
            response = new RecommendMealResponse
            {
                Items = aiResponse.Data.Items
                    .Select(item => new RecommendMealResponseItem
                    {
                        RecipeId = item.RecipeId,
                        RecipeName = item.RecipeName,
                        ImageUrl = candidateRecipes.FirstOrDefault(recipe => recipe.RecipeId == item.RecipeId)?.ImageUrl,
                        MatchScore = item.MatchScore,
                        MissingIngredientCount = item.MissingIngredientCount,
                        MissingIngredientNames = item.MissingIngredientNames,
                        Reason = item.Reason,
                        Rank = item.Rank
                    })
                    .ToList()
            };
            message = "Meal recommendations generated.";
        }
        else
        {
            response = await BuildLocalRecommendMealResponseAsync(
                userIngredients,
                candidateRecipes,
                request.TopK,
                cancellationToken);
            message = string.IsNullOrWhiteSpace(aiResponse.Message)
                ? "AI service unavailable. Local fallback applied."
                : $"{aiResponse.Message} Local fallback applied.";
        }

        recommendation.Status = "completed";
        recommendation.CompletedAt = DateTime.UtcNow;
        recommendation.UpdatedAt = DateTime.UtcNow;

        foreach (var item in response.Items)
        {
            var candidateRecipe = candidateRecipes.FirstOrDefault(recipe => recipe.RecipeId == item.RecipeId);

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

            item.ImageUrl ??= candidateRecipe?.ImageUrl;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponse<RecommendMealResponse>.SuccessResponse(response, message);
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

    public async Task<ApiResponse<MealIngredientCheckResponse>> CheckMealIngredientsAsync(
        Guid userId,
        Guid mealId,
        CancellationToken cancellationToken = default)
    {
        var recipe = await _dbContext.Recipes
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == mealId && !item.IsDeleted, cancellationToken);

        if (recipe is null)
        {
            return ApiResponse<MealIngredientCheckResponse>.Fail("Recipe not found.");
        }

        var requiredIngredients = await LoadRecipeIngredientItemsAsync(recipe.Id, cancellationToken);
        if (requiredIngredients.Count == 0)
        {
            return ApiResponse<MealIngredientCheckResponse>.Fail("Recipe does not have configured ingredients.");
        }

        var fridgeIngredients = await LoadUserPantryIngredientItemsAsync(userId, cancellationToken);
        var aiResponse = await _aiRecommendationClient.CheckMealIngredientsAsync(
            new MealIngredientCheckAiRequest
            {
                UserId = userId,
                Meal = new MealIngredientCheckAiMeal
                {
                    MealId = recipe.Id,
                    MealName = recipe.Name
                },
                RequiredIngredients = requiredIngredients.Select(ToAiIngredient).ToList(),
                FridgeIngredients = fridgeIngredients.Select(ToAiIngredient).ToList()
            },
            cancellationToken);

        var fallbackResponse = BuildMealIngredientCheckResponse(recipe, requiredIngredients, fridgeIngredients);
        if (!aiResponse.Success || aiResponse.Data is null)
        {
            return ApiResponse<MealIngredientCheckResponse>.SuccessResponse(
                fallbackResponse,
                string.IsNullOrWhiteSpace(aiResponse.Message)
                    ? "Meal ingredients checked using local fallback."
                    : $"{aiResponse.Message} Local fallback applied.");
        }

        return ApiResponse<MealIngredientCheckResponse>.SuccessResponse(
            new MealIngredientCheckResponse
            {
                MealId = aiResponse.Data.MealId,
                MealName = aiResponse.Data.MealName,
                AvailableIngredients = aiResponse.Data.AvailableIngredients
                    .Select(ToResponseItem)
                    .ToList(),
                MissingIngredients = aiResponse.Data.MissingIngredients
                    .Select(ToResponseItem)
                    .ToList(),
                Note = aiResponse.Data.Note
            },
            "Meal ingredients checked.");
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

        var recipeImageUrls = await _dbContext.Recipes
            .AsNoTracking()
            .Where(recipe => recommendationItems.Select(item => item.RecipeId).Contains(recipe.Id))
            .ToDictionaryAsync(recipe => recipe.Id, recipe => recipe.ImageUrl, cancellationToken);

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
                ImageUrl = recipeImageUrls.GetValueOrDefault(item.RecipeId),
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
                ImageUrl = recipe.ImageUrl,
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

    private async Task<RecommendMealResponse> BuildLocalRecommendMealResponseAsync(
        IReadOnlyList<AiIngredientItem> userIngredients,
        IReadOnlyList<AiCandidateRecipeItem> candidateRecipes,
        int topK,
        CancellationToken cancellationToken)
    {
        var userIngredientNames = userIngredients
            .Select(item => NormalizeName(item.Name))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var items = new List<RecommendMealResponseItem>();

        foreach (var recipe in candidateRecipes)
        {
            var requiredIngredientNames = recipe.IngredientNames
                .Select(NormalizeName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var matchedNames = requiredIngredientNames
                .Where(userIngredientNames.Contains)
                .ToList();

            var missingNames = requiredIngredientNames
                .Where(name => !userIngredientNames.Contains(name))
                .ToList();

            var matchScore = requiredIngredientNames.Count == 0
                ? 0m
                : Math.Round((decimal)matchedNames.Count / requiredIngredientNames.Count, 3);

            items.Add(new RecommendMealResponseItem
            {
                RecipeId = recipe.RecipeId,
                RecipeName = recipe.RecipeName,
                ImageUrl = recipe.ImageUrl,
                MatchScore = matchScore,
                MissingIngredientCount = missingNames.Count,
                MissingIngredientNames = missingNames.Take(5).ToList(),
                Reason = $"Matched {matchedNames.Count} of {requiredIngredientNames.Count} required ingredients using local fallback.",
                Rank = 0
            });
        }

        var rankedItems = items
            .OrderByDescending(item => item.MatchScore)
            .ThenBy(item => item.MissingIngredientCount)
            .ThenBy(item => item.RecipeName, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(topK, 0))
            .ToList();

        for (var index = 0; index < rankedItems.Count; index++)
        {
            rankedItems[index].Rank = index + 1;
        }

        return new RecommendMealResponse
        {
            Items = rankedItems
        };
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

    private async Task<List<MealIngredientCheckAiIngredient>> LoadRecipeIngredientItemsAsync(
        Guid recipeId,
        CancellationToken cancellationToken)
        => await (
                from recipeIngredient in _dbContext.RecipeIngredients.AsNoTracking()
                join ingredient in _dbContext.Ingredients.AsNoTracking()
                    on recipeIngredient.IngredientId equals ingredient.Id
                where recipeIngredient.RecipeId == recipeId
                    && !recipeIngredient.IsDeleted
                    && !ingredient.IsDeleted
                select new MealIngredientCheckAiIngredient
                {
                    IngredientId = ingredient.Id,
                    Name = ingredient.Name,
                    Quantity = recipeIngredient.Quantity,
                    Unit = recipeIngredient.Unit
                })
            .ToListAsync(cancellationToken);

    private async Task<List<MealIngredientCheckAiIngredient>> LoadUserPantryIngredientItemsAsync(
        Guid userId,
        CancellationToken cancellationToken)
        => await (
                from pantryItem in _dbContext.UserPantryItems.AsNoTracking()
                join ingredient in _dbContext.Ingredients.AsNoTracking()
                    on pantryItem.IngredientId equals ingredient.Id
                where pantryItem.UserId == userId
                    && !pantryItem.IsDeleted
                    && !ingredient.IsDeleted
                select new MealIngredientCheckAiIngredient
                {
                    IngredientId = ingredient.Id,
                    Name = ingredient.Name,
                    Quantity = pantryItem.Quantity,
                    Unit = pantryItem.Unit
                })
            .ToListAsync(cancellationToken);

    private static MealIngredientCheckAiIngredient ToAiIngredient(MealIngredientCheckAiIngredient item)
        => new()
        {
            IngredientId = item.IngredientId,
            Name = item.Name,
            Quantity = item.Quantity,
            Unit = item.Unit
        };

    private static MealIngredientCheckItem ToResponseItem(MealIngredientCheckAiIngredient item)
        => new()
        {
            IngredientId = item.IngredientId ?? Guid.Empty,
            Name = item.Name,
            Quantity = item.Quantity,
            Unit = item.Unit
        };

    private static MealIngredientCheckResponse BuildMealIngredientCheckResponse(
        Recipe recipe,
        IReadOnlyCollection<MealIngredientCheckAiIngredient> requiredIngredients,
        IReadOnlyCollection<MealIngredientCheckAiIngredient> fridgeIngredients)
    {
        var fridgeIngredientIds = fridgeIngredients
            .Where(item => item.IngredientId.HasValue && item.IngredientId.Value != Guid.Empty)
            .Select(item => item.IngredientId!.Value)
            .ToHashSet();

        var fridgeNormalizedNames = fridgeIngredients
            .Where(item => !string.IsNullOrWhiteSpace(item.Name))
            .Select(item => NormalizeName(item.Name))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var available = new List<MealIngredientCheckItem>();
        var missing = new List<MealIngredientCheckItem>();

        foreach (var ingredient in requiredIngredients)
        {
            var hasIngredient = ingredient.IngredientId.HasValue
                && ingredient.IngredientId.Value != Guid.Empty
                && fridgeIngredientIds.Contains(ingredient.IngredientId.Value);

            if (!hasIngredient && !string.IsNullOrWhiteSpace(ingredient.Name))
            {
                hasIngredient = fridgeNormalizedNames.Contains(NormalizeName(ingredient.Name));
            }

            var item = new MealIngredientCheckItem
            {
                IngredientId = ingredient.IngredientId ?? Guid.Empty,
                Name = ingredient.Name,
                Quantity = ingredient.Quantity,
                Unit = ingredient.Unit
            };

            if (hasIngredient)
            {
                available.Add(item);
            }
            else
            {
                missing.Add(item);
            }
        }

        return new MealIngredientCheckResponse
        {
            MealId = recipe.Id,
            MealName = recipe.Name,
            AvailableIngredients = available,
            MissingIngredients = missing,
            Note = missing.Count == 0
                ? "You already have all required ingredients for this meal."
                : $"You are missing {missing.Count} ingredient(s) for this meal."
        };
    }

    private static string NormalizeName(string value)
        => string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();

    private static List<string> SplitNames(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}
