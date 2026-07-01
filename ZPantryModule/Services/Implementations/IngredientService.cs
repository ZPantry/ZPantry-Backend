using AuthenticationModule.Contracts.Common;
using AuthenticationModule.DTOs;
using AuthenticationModule.Repositories.Entities;
using Microsoft.EntityFrameworkCore;
using ZPantryModule.Services.Interfaces;

namespace ZPantryModule.Services.Implementations;

public class IngredientService : IIngredientService
{
    private readonly ZpantryDbContext _dbContext;
    private readonly IAIRecommendationClient _aiRecommendationClient;

    public IngredientService(ZpantryDbContext dbContext, IAIRecommendationClient aiRecommendationClient)
    {
        _dbContext = dbContext;
        _aiRecommendationClient = aiRecommendationClient;
    }

    public async Task<PagedResponse<IngredientDto>> GetAllAsync(
        int pageIndex,
        int pageSize,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var paging = ZPantryMappings.NormalizePaging(pageIndex, pageSize);
        var query = _dbContext.Ingredients
            .AsNoTracking()
            .Where(ingredient => !ingredient.IsDeleted)
            .OrderBy(ingredient => ingredient.Name);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = ZPantryMappings.NormalizeName(search);
            query = query.Where(ingredient =>
                ingredient.Name.ToLower().Contains(keyword)
                || ingredient.NormalizedName.ToLower().Contains(keyword)
                || (ingredient.Category != null && ingredient.Category.ToLower().Contains(keyword)))
                .OrderBy(ingredient => ingredient.Name);
        }

        var totalItems = await query.CountAsync(cancellationToken);
        var ingredients = await query
            .Skip((paging.PageIndex - 1) * paging.PageSize)
            .Take(paging.PageSize)
            .ToListAsync(cancellationToken);

        return PagedResponse<IngredientDto>.SuccessPage(
            ingredients.Select(ingredient => ingredient.ToDto()),
            paging.PageIndex,
            paging.PageSize,
            totalItems);
    }

    public async Task<ApiResponse<IngredientDto>> CreateAsync(
        CreateIngredientRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ApiResponse<IngredientDto>.Fail("Ingredient name is required.");
        }

        var normalizedName = ZPantryMappings.NormalizeName(request.Name);
        var exists = await _dbContext.Ingredients.AnyAsync(
            ingredient => !ingredient.IsDeleted && ingredient.NormalizedName == normalizedName,
            cancellationToken);

        if (exists)
        {
            return ApiResponse<IngredientDto>.Fail("Ingredient already exists.");
        }

        var ingredient = new Ingredient
        {
            Name = request.Name.Trim(),
            NormalizedName = normalizedName,
            Category = request.Category,
            Unit = request.Unit,
            CaloriesPerUnit = request.CaloriesPerUnit,
            ProteinPerUnit = request.ProteinPerUnit,
            FatPerUnit = request.FatPerUnit,
            CarbPerUnit = request.CarbPerUnit,
            ImageUrl = request.ImageUrl
        };

        var embeddingResponse = await _aiRecommendationClient.EmbedIngredientAsync(
            new EmbedIngredientAiRequest
            {
                IngredientId = ingredient.Id,
                Name = ingredient.Name,
                NormalizedName = ingredient.NormalizedName,
                Category = ingredient.Category
            },
            cancellationToken);

        if (embeddingResponse.Success && embeddingResponse.Data?.Embedding.Count > 0)
        {
            ingredient.Embedding = embeddingResponse.Data.Embedding.ToArray();
        }

        _dbContext.Ingredients.Add(ingredient);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponse<IngredientDto>.SuccessResponse(ingredient.ToDto(), "Ingredient created.");
    }

    public async Task<ApiResponse<IngredientDto>> UpdateAsync(
        Guid id,
        UpdateIngredientRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ApiResponse<IngredientDto>.Fail("Ingredient name is required.");
        }

        var ingredient = await _dbContext.Ingredients
            .FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted, cancellationToken);

        if (ingredient is null)
        {
            return ApiResponse<IngredientDto>.Fail("Ingredient not found.");
        }

        var normalizedName = ZPantryMappings.NormalizeName(request.Name);
        var duplicateExists = await _dbContext.Ingredients.AnyAsync(
            item => item.Id != id && !item.IsDeleted && item.NormalizedName == normalizedName,
            cancellationToken);

        if (duplicateExists)
        {
            return ApiResponse<IngredientDto>.Fail("Ingredient already exists.");
        }

        ingredient.Name = request.Name.Trim();
        ingredient.NormalizedName = normalizedName;
        ingredient.Category = request.Category;
        ingredient.Unit = request.Unit;
        ingredient.CaloriesPerUnit = request.CaloriesPerUnit;
        ingredient.ProteinPerUnit = request.ProteinPerUnit;
        ingredient.FatPerUnit = request.FatPerUnit;
        ingredient.CarbPerUnit = request.CarbPerUnit;
        ingredient.ImageUrl = request.ImageUrl;
        ingredient.UpdatedAt = DateTime.UtcNow;

        var embeddingResponse = await _aiRecommendationClient.EmbedIngredientAsync(
            new EmbedIngredientAiRequest
            {
                IngredientId = ingredient.Id,
                Name = ingredient.Name,
                NormalizedName = ingredient.NormalizedName,
                Category = ingredient.Category
            },
            cancellationToken);

        if (embeddingResponse.Success && embeddingResponse.Data?.Embedding.Count > 0)
        {
            ingredient.Embedding = embeddingResponse.Data.Embedding.ToArray();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponse<IngredientDto>.SuccessResponse(ingredient.ToDto(), "Ingredient updated.");
    }

    public async Task<ApiResponse<object>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var ingredient = await _dbContext.Ingredients
            .FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted, cancellationToken);

        if (ingredient is null)
        {
            return ApiResponse<object>.Fail("Ingredient not found.");
        }

        ingredient.SoftDelete();
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponse<object>.SuccessResponse(null, "Ingredient deleted.");
    }
}
