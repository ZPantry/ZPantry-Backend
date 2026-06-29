using AuthenticationModule.Contracts.Common;
using AuthenticationModule.DTOs;
using AuthenticationModule.Repositories.Entities;
using Microsoft.EntityFrameworkCore;
using ZPantryModule.Services.Interfaces;

namespace ZPantryModule.Services.Implementations;

public class RecipeService : IRecipeService
{
    private readonly ZpantryDbContext _dbContext;
    private readonly IAIRecommendationClient _aiRecommendationClient;

    public RecipeService(ZpantryDbContext dbContext, IAIRecommendationClient aiRecommendationClient)
    {
        _dbContext = dbContext;
        _aiRecommendationClient = aiRecommendationClient;
    }

    public async Task<PagedResponse<RecipeDto>> GetAllAsync(
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var paging = ZPantryMappings.NormalizePaging(pageIndex, pageSize);
        var query = _dbContext.Recipes
            .AsNoTracking()
            .Where(recipe => !recipe.IsDeleted)
            .OrderBy(recipe => recipe.Name);

        var totalItems = await query.CountAsync(cancellationToken);
        var recipes = await query
            .Skip((paging.PageIndex - 1) * paging.PageSize)
            .Take(paging.PageSize)
            .ToListAsync(cancellationToken);

        return PagedResponse<RecipeDto>.SuccessPage(
            recipes.Select(recipe => recipe.ToDto()),
            paging.PageIndex,
            paging.PageSize,
            totalItems);
    }

    public async Task<ApiResponse<RecipeDto>> CreateAsync(
        CreateRecipeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ApiResponse<RecipeDto>.Fail("Recipe name is required.");
        }

        var recipe = new Recipe
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            CookingTimeMinutes = request.CookingTimeMinutes,
            Difficulty = request.Difficulty,
            ServingSize = request.ServingSize,
            InstructionText = request.InstructionText,
            ImageUrl = request.ImageUrl,
            SourceType = request.SourceType
        };

        AddRecipeIngredients(recipe.Id, request.Ingredients);
        await ApplyRecipeEmbeddingAsync(recipe, request.Ingredients, cancellationToken);

        _dbContext.Recipes.Add(recipe);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponse<RecipeDto>.SuccessResponse(recipe.ToDto(), "Recipe created.");
    }

    public async Task<ApiResponse<RecipeDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var recipe = await _dbContext.Recipes
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted, cancellationToken);

        if (recipe is null)
        {
            return ApiResponse<RecipeDto>.Fail("Recipe not found.");
        }

        return ApiResponse<RecipeDto>.SuccessResponse(recipe.ToDto());
    }

    public async Task<ApiResponse<RecipeDto>> UpdateAsync(
        Guid id,
        UpdateRecipeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ApiResponse<RecipeDto>.Fail("Recipe name is required.");
        }

        var recipe = await _dbContext.Recipes
            .FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted, cancellationToken);

        if (recipe is null)
        {
            return ApiResponse<RecipeDto>.Fail("Recipe not found.");
        }

        recipe.Name = request.Name.Trim();
        recipe.Description = request.Description;
        recipe.CookingTimeMinutes = request.CookingTimeMinutes;
        recipe.Difficulty = request.Difficulty;
        recipe.ServingSize = request.ServingSize;
        recipe.InstructionText = request.InstructionText;
        recipe.ImageUrl = request.ImageUrl;
        recipe.SourceType = request.SourceType;
        recipe.UpdatedAt = DateTime.UtcNow;

        var existingIngredients = await _dbContext.RecipeIngredients
            .Where(item => item.RecipeId == recipe.Id && !item.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var existingIngredient in existingIngredients)
        {
            existingIngredient.SoftDelete();
        }

        AddRecipeIngredients(recipe.Id, request.Ingredients);
        await ApplyRecipeEmbeddingAsync(recipe, request.Ingredients, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponse<RecipeDto>.SuccessResponse(recipe.ToDto(), "Recipe updated.");
    }

    public async Task<ApiResponse<object>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var recipe = await _dbContext.Recipes
            .FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted, cancellationToken);

        if (recipe is null)
        {
            return ApiResponse<object>.Fail("Recipe not found.");
        }

        recipe.SoftDelete();

        var recipeIngredients = await _dbContext.RecipeIngredients
            .Where(item => item.RecipeId == id && !item.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var recipeIngredient in recipeIngredients)
        {
            recipeIngredient.SoftDelete();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponse<object>.SuccessResponse(null, "Recipe deleted.");
    }

    private void AddRecipeIngredients(Guid recipeId, IEnumerable<RecipeIngredientDto> ingredients)
    {
        foreach (var item in ingredients)
        {
            _dbContext.RecipeIngredients.Add(new RecipeIngredient
            {
                RecipeId = recipeId,
                IngredientId = item.IngredientId,
                Quantity = item.Quantity,
                Unit = item.Unit,
                IsRequired = item.IsRequired,
                Note = item.Note
            });
        }
    }

    private async Task ApplyRecipeEmbeddingAsync(
        Recipe recipe,
        IEnumerable<RecipeIngredientDto> recipeIngredients,
        CancellationToken cancellationToken)
    {
        var ingredientIds = recipeIngredients
            .Select(item => item.IngredientId)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        var ingredientNames = await _dbContext.Ingredients
            .AsNoTracking()
            .Where(ingredient => ingredientIds.Contains(ingredient.Id) && !ingredient.IsDeleted)
            .Select(ingredient => ingredient.Name)
            .ToListAsync(cancellationToken);

        var embeddingResponse = await _aiRecommendationClient.EmbedRecipeAsync(
            new EmbedRecipeAiRequest
            {
                RecipeId = recipe.Id,
                Name = recipe.Name,
                Description = recipe.Description,
                IngredientNames = ingredientNames,
                InstructionText = recipe.InstructionText
            },
            cancellationToken);

        if (embeddingResponse.Success && embeddingResponse.Data?.Embedding.Count > 0)
        {
            recipe.Embedding = embeddingResponse.Data.Embedding.ToArray();
        }
    }
}
