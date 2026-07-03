using AuthenticationModule.Contracts.Common;
using AuthenticationModule.DTOs;
using AuthenticationModule.Repositories.Entities;
using ZPantryModule.DTOs;
using Microsoft.EntityFrameworkCore;
using ZPantryModule.Services.Interfaces;

namespace ZPantryModule.Services.Implementations;

public class IngredientService : IIngredientService
{
    private readonly ZpantryDbContext _dbContext;
    private readonly IAIRecommendationClient _aiRecommendationClient;
    private readonly ICloudinaryStorageService _cloudinaryStorageService;

    public IngredientService(
        ZpantryDbContext dbContext,
        IAIRecommendationClient aiRecommendationClient,
        ICloudinaryStorageService cloudinaryStorageService)
    {
        _dbContext = dbContext;
        _aiRecommendationClient = aiRecommendationClient;
        _cloudinaryStorageService = cloudinaryStorageService;
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
            ImageUrl = request.ImageUrl,
            GradientFrom = request.GradientFrom ?? ColorGradient.Generate(request.Name, request.Category).From,
            GradientTo = request.GradientTo ?? ColorGradient.Generate(request.Name, request.Category).To
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

    public async Task<ApiResponse<IngredientDto>> CreateV2Async(
        CreateIngredientFormRequest request,
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
            ImageUrl = request.ImageUrl,
            GradientFrom = request.GradientFrom ?? ColorGradient.Generate(request.Name, request.Category).From,
            GradientTo = request.GradientTo ?? ColorGradient.Generate(request.Name, request.Category).To
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

        if (request.ImageFile is not null && request.ImageFile.Length > 0)
        {
            await using var stream = request.ImageFile.OpenReadStream();
            var uploadResponse = await _cloudinaryStorageService.UploadAsync(
                stream,
                request.ImageFile.FileName,
                ingredientId: ingredient.Id,
                cancellationToken: cancellationToken);

            if (!uploadResponse.Success || string.IsNullOrWhiteSpace(uploadResponse.Data))
            {
                return ApiResponse<IngredientDto>.Fail(uploadResponse.Message ?? "Ingredient image upload failed.");
            }

            ingredient.ImageUrl = uploadResponse.Data;
            ingredient.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return ApiResponse<IngredientDto>.SuccessResponse(ingredient.ToDto(), "Ingredient created.");
    }

    public async Task<ApiResponse<IngredientDto>> UpdateAsync(
        Guid id,
        UpdateIngredientRequest request,
        CancellationToken cancellationToken = default)
    {
        var ingredient = await _dbContext.Ingredients
            .FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted, cancellationToken);

        if (ingredient is null)
        {
            return ApiResponse<IngredientDto>.Fail("Ingredient not found.");
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
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
        }

        if (request.Category != null)
        {
            ingredient.Category = request.Category;
        }

        if (request.Unit != null)
        {
            ingredient.Unit = request.Unit;
        }

        if (request.CaloriesPerUnit.HasValue)
        {
            ingredient.CaloriesPerUnit = request.CaloriesPerUnit;
        }

        if (request.ProteinPerUnit.HasValue)
        {
            ingredient.ProteinPerUnit = request.ProteinPerUnit;
        }

        if (request.FatPerUnit.HasValue)
        {
            ingredient.FatPerUnit = request.FatPerUnit;
        }

        if (request.CarbPerUnit.HasValue)
        {
            ingredient.CarbPerUnit = request.CarbPerUnit;
        }

        if (request.ImageUrl != null)
        {
            ingredient.ImageUrl = request.ImageUrl;
        }

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

    public async Task<ApiResponse<IngredientDto>> UpdateV2Async(
        Guid id,
        UpdateIngredientFormRequest request,
        CancellationToken cancellationToken = default)
    {
        var ingredient = await _dbContext.Ingredients
            .FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted, cancellationToken);

        if (ingredient is null)
        {
            return ApiResponse<IngredientDto>.Fail("Ingredient not found.");
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
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
        }

        if (request.Category != null)
        {
            ingredient.Category = request.Category;
        }

        if (request.Unit != null)
        {
            ingredient.Unit = request.Unit;
        }

        if (request.CaloriesPerUnit.HasValue)
        {
            ingredient.CaloriesPerUnit = request.CaloriesPerUnit;
        }

        if (request.ProteinPerUnit.HasValue)
        {
            ingredient.ProteinPerUnit = request.ProteinPerUnit;
        }

        if (request.FatPerUnit.HasValue)
        {
            ingredient.FatPerUnit = request.FatPerUnit;
        }

        if (request.CarbPerUnit.HasValue)
        {
            ingredient.CarbPerUnit = request.CarbPerUnit;
        }

        if (request.GradientFrom != null)
        {
            ingredient.GradientFrom = request.GradientFrom;
        }

        if (request.GradientTo != null)
        {
            ingredient.GradientTo = request.GradientTo;
        }

        if (request.ImageUrl != null)
        {
            ingredient.ImageUrl = request.ImageUrl;
        }

        ingredient.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (request.ImageFile is not null && request.ImageFile.Length > 0)
        {
            await using var stream = request.ImageFile.OpenReadStream();
            var uploadResponse = await _cloudinaryStorageService.UploadAsync(
                stream,
                request.ImageFile.FileName,
                ingredientId: ingredient.Id,
                cancellationToken: cancellationToken);

            if (!uploadResponse.Success || string.IsNullOrWhiteSpace(uploadResponse.Data))
            {
                return ApiResponse<IngredientDto>.Fail(uploadResponse.Message ?? "Ingredient image upload failed.");
            }

            ingredient.ImageUrl = uploadResponse.Data;
            ingredient.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

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
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

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
