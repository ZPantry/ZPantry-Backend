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
        var ingredientsByRecipeId = await LoadRecipeIngredientsAsync(
            recipes.Select(recipe => recipe.Id).ToList(),
            cancellationToken);

        return PagedResponse<RecipeDto>.SuccessPage(
            recipes.Select(recipe =>
            {
                var dto = recipe.ToDto();
                dto.Ingredients = ingredientsByRecipeId.TryGetValue(recipe.Id, out var recipeIngredients)
                    ? recipeIngredients
                    : [];
                return dto;
            }),
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
            SourceType = request.SourceType,
            GradientFrom = request.GradientFrom ?? ColorGradient.Generate(request.Name, request.Difficulty).From,
            GradientTo = request.GradientTo ?? ColorGradient.Generate(request.Name, request.Difficulty).To
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

        var dto = recipe.ToDto();
        var ingredientsByRecipeId = await LoadRecipeIngredientsAsync(new[] { recipe.Id }, cancellationToken);
        dto.Ingredients = ingredientsByRecipeId.TryGetValue(recipe.Id, out var recipeIngredients)
            ? recipeIngredients
            : [];

        return ApiResponse<RecipeDto>.SuccessResponse(dto);
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
        recipe.GradientFrom = request.GradientFrom ?? ColorGradient.Generate(request.Name, request.Difficulty).From;
        recipe.GradientTo = request.GradientTo ?? ColorGradient.Generate(request.Name, request.Difficulty).To;
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

    private async Task<Dictionary<Guid, List<RecipeIngredientDto>>> LoadRecipeIngredientsAsync(
        IReadOnlyCollection<Guid> recipeIds,
        CancellationToken cancellationToken)
    {
        var recipeIdSet = recipeIds.Distinct().ToArray();

        if (recipeIdSet.Length == 0)
        {
            return new Dictionary<Guid, List<RecipeIngredientDto>>();
        }

        var ingredientRows = await (
            from recipeIngredient in _dbContext.RecipeIngredients.AsNoTracking()
            join ingredient in _dbContext.Ingredients.AsNoTracking()
                on recipeIngredient.IngredientId equals ingredient.Id
            where recipeIdSet.Contains(recipeIngredient.RecipeId)
                && !recipeIngredient.IsDeleted
                && !ingredient.IsDeleted
            select new
            {
                recipeIngredient.RecipeId,
                Item = new RecipeIngredientDto
                {
                    IngredientId = ingredient.Id,
                    IngredientName = ingredient.Name,
                    Quantity = recipeIngredient.Quantity,
                    Unit = recipeIngredient.Unit,
                    IsRequired = recipeIngredient.IsRequired,
                    Note = recipeIngredient.Note
                }
            })
            .ToListAsync(cancellationToken);

        return ingredientRows
            .GroupBy(row => row.RecipeId)
            .ToDictionary(group => group.Key, group => group.Select(row => row.Item).ToList());
    }
}
