using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AuthenticationModule.Contracts.Common;
using AuthenticationModule.DTOs;
using AuthenticationModule.Repositories.Entities;
using Microsoft.EntityFrameworkCore;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using ZPantryModule.Services.Interfaces;

namespace ZPantryModule.Services.Implementations;

public class CloudinaryStorageService : ICloudinaryStorageService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ZpantryDbContext _dbContext;

    public CloudinaryStorageService(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ZpantryDbContext dbContext)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _dbContext = dbContext;
    }

    public async Task<ApiResponse<MediaUploadResultDto>> UploadDetailedAsync(
        Stream fileStream,
        string fileName,
        Guid? ingredientId = null,
        Guid? recipeId = null,
        CancellationToken cancellationToken = default)
    {
        var settings = GetSettings();
        if (settings is null)
        {
            return ApiResponse<MediaUploadResultDto>.Fail("Cloudinary configuration is missing.");
        }

        if (ingredientId.HasValue && recipeId.HasValue)
        {
            return ApiResponse<MediaUploadResultDto>.Fail("Upload can be linked to either an ingredient or a recipe, not both.");
        }

        if (fileStream.CanSeek && fileStream.Length == 0)
        {
            return ApiResponse<MediaUploadResultDto>.Fail("Uploaded file is empty.");
        }

        try
        {
            var linkedIngredient = ingredientId.HasValue
                ? await _dbContext.Ingredients.FirstOrDefaultAsync(
                    ingredient => ingredient.Id == ingredientId.Value && !ingredient.IsDeleted,
                    cancellationToken)
                : null;

            if (ingredientId.HasValue && linkedIngredient is null)
            {
                return ApiResponse<MediaUploadResultDto>.Fail("Ingredient not found.");
            }

            var linkedRecipe = recipeId.HasValue
                ? await _dbContext.Recipes.FirstOrDefaultAsync(
                    recipe => recipe.Id == recipeId.Value && !recipe.IsDeleted,
                    cancellationToken)
                : null;

            if (recipeId.HasValue && linkedRecipe is null)
            {
                return ApiResponse<MediaUploadResultDto>.Fail("Recipe not found.");
            }

            var result = await UploadToCloudinaryAsync(fileStream, fileName, cancellationToken);
            if (!result.Success || result.Data is null)
            {
                return ApiResponse<MediaUploadResultDto>.Fail(result.Message, result.Errors, result.TraceId);
            }

            var uploadResult = result.Data;
            var mediaAsset = new MediaAsset
            {
                IngredientId = ingredientId,
                RecipeId = recipeId,
                PublicId = uploadResult.PublicId,
                Url = uploadResult.Url,
                SecureUrl = uploadResult.SecureUrl,
                ResourceType = uploadResult.ResourceType,
                Format = uploadResult.Format,
                Width = uploadResult.Width,
                Height = uploadResult.Height
            };

            _dbContext.MediaAssets.Add(mediaAsset);

            if (linkedIngredient is not null)
            {
                linkedIngredient.ImageUrl = uploadResult.SecureUrl;
                linkedIngredient.UpdatedAt = DateTime.UtcNow;
            }

            if (linkedRecipe is not null)
            {
                linkedRecipe.ImageUrl = uploadResult.SecureUrl;
                linkedRecipe.UpdatedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            return ApiResponse<MediaUploadResultDto>.SuccessResponse(
                new MediaUploadResultDto
                {
                    MediaAssetId = mediaAsset.Id,
                    PublicId = mediaAsset.PublicId,
                    Url = mediaAsset.Url,
                    SecureUrl = mediaAsset.SecureUrl,
                    ResourceType = mediaAsset.ResourceType,
                    Format = mediaAsset.Format,
                    Width = mediaAsset.Width,
                    Height = mediaAsset.Height
                },
                "Media uploaded.");
        }
        catch (Exception ex)
        {
            return ApiResponse<MediaUploadResultDto>.Fail($"Exception during upload: {ex.Message}");
        }
    }

    public async Task<ApiResponse<string>> UploadAsync(
        Stream fileStream,
        string fileName,
        Guid? ingredientId = null,
        Guid? recipeId = null,
        CancellationToken cancellationToken = default)
    {
        var detailed = await UploadDetailedAsync(fileStream, fileName, ingredientId, recipeId, cancellationToken);
        if (!detailed.Success || detailed.Data is null)
        {
            return ApiResponse<string>.Fail(detailed.Message, detailed.Errors, detailed.TraceId);
        }

        return ApiResponse<string>.SuccessResponse(detailed.Data.SecureUrl, detailed.Message);
    }

    public async Task<ApiResponse<object>> DeleteAsync(string publicId, CancellationToken cancellationToken = default)
    {
        var settings = GetSettings();
        if (settings is null)
        {
            return ApiResponse<object>.Fail("Cloudinary configuration is missing.");
        }

        if (string.IsNullOrWhiteSpace(publicId))
        {
            return ApiResponse<object>.Fail("PublicId is required.");
        }

        try
        {
            var mediaAsset = Guid.TryParse(publicId, out var mediaAssetId)
                ? await _dbContext.MediaAssets.FirstOrDefaultAsync(
                    asset => asset.Id == mediaAssetId && !asset.IsDeleted,
                    cancellationToken)
                : await _dbContext.MediaAssets.FirstOrDefaultAsync(
                    asset => asset.PublicId == publicId && !asset.IsDeleted,
                    cancellationToken);

            var cloudinary = GetCloudinaryClient();
            var deletionParams = new DeletionParams(mediaAsset?.PublicId ?? publicId);
            var result = await cloudinary.DestroyAsync(deletionParams);

            if (result.Result == "ok" || result.Result == "not found")
            {
                if (mediaAsset is not null)
                {
                    await ClearLinkedImageAsync(mediaAsset, cancellationToken);
                    mediaAsset.SoftDelete();
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                return ApiResponse<object>.SuccessResponse(null, "File deleted successfully.");
            }

            return ApiResponse<object>.Fail($"Cloudinary delete failed: {result.Result}");
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.Fail($"Exception during delete: {ex.Message}");
        }
    }

    private Cloudinary GetCloudinaryClient()
    {
        var account = new Account(GetCloudName(), GetApiKey(), GetApiSecret());
        return new Cloudinary(account);
    }

    private async Task<ApiResponse<MediaUploadResultDto>> UploadToCloudinaryAsync(
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cloudinary = GetCloudinaryClient();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, fileStream)
            };

            var preset = GetPresetName();
            if (!string.IsNullOrWhiteSpace(preset))
            {
                uploadParams.UploadPreset = preset;
            }
            else
            {
                uploadParams.Folder = "zpantry";
            }

            var uploadResult = await cloudinary.UploadAsync(uploadParams, cancellationToken);

            if (uploadResult.Error != null)
            {
                return ApiResponse<MediaUploadResultDto>.Fail($"Cloudinary upload failed: {uploadResult.Error.Message}");
            }

            var secureUrl = uploadResult.SecureUrl?.ToString();
            if (string.IsNullOrWhiteSpace(secureUrl))
            {
                return ApiResponse<MediaUploadResultDto>.Fail("Cloudinary upload response did not contain a secure URL.");
            }

            return ApiResponse<MediaUploadResultDto>.SuccessResponse(
                new MediaUploadResultDto
                {
                    PublicId = uploadResult.PublicId ?? string.Empty,
                    Url = uploadResult.Url?.ToString() ?? string.Empty,
                    SecureUrl = secureUrl,
                    ResourceType = uploadResult.ResourceType,
                    Format = uploadResult.Format,
                    Width = uploadResult.Width,
                    Height = uploadResult.Height
                },
                "Media uploaded.");
        }
        catch (Exception ex)
        {
            return ApiResponse<MediaUploadResultDto>.Fail($"Exception during upload: {ex.Message}");
        }
    }

    private string GetCloudName()
        => _configuration["Cloudinary_Name"] ?? _configuration["CLOUDINARY_CLOUD_NAME"] ?? _configuration["Cloudinary:Name"] ?? string.Empty;

    private string GetApiKey()
        => _configuration["Cloudinary_API_Key"] ?? _configuration["CLOUDINARY_API_KEY"] ?? _configuration["Cloudinary:ApiKey"] ?? string.Empty;

    private string GetApiSecret()
        => _configuration["Cloudinary_API_Secret"] ?? _configuration["CLOUDINARY_API_SECRET"] ?? _configuration["Cloudinary:ApiSecret"] ?? string.Empty;

    private string GetPresetName()
        => _configuration["Cloudinary_PresetName"] ?? _configuration["CLOUDINARY_PRESET_NAME"] ?? _configuration["Cloudinary:PresetName"] ?? string.Empty;

    private CloudinarySettings? GetSettings()
    {
        var cloudName = GetCloudName();
        var apiKey = GetApiKey();
        var apiSecret = GetApiSecret();

        if (string.IsNullOrWhiteSpace(cloudName)
            || string.IsNullOrWhiteSpace(apiKey)
            || string.IsNullOrWhiteSpace(apiSecret))
        {
            return null;
        }

        return new CloudinarySettings(cloudName, apiKey, apiSecret);
    }

    private bool IsConfigured()
        => !string.IsNullOrWhiteSpace(GetCloudName())
            && !string.IsNullOrWhiteSpace(GetApiKey())
            && !string.IsNullOrWhiteSpace(GetApiSecret());

    private async Task ClearLinkedImageAsync(MediaAsset mediaAsset, CancellationToken cancellationToken)
    {
        if (mediaAsset.IngredientId.HasValue)
        {
            var ingredient = await _dbContext.Ingredients.FirstOrDefaultAsync(
                item => item.Id == mediaAsset.IngredientId.Value && !item.IsDeleted,
                cancellationToken);

            if (ingredient?.ImageUrl == mediaAsset.SecureUrl)
            {
                ingredient.ImageUrl = null;
                ingredient.UpdatedAt = DateTime.UtcNow;
            }
        }

        if (mediaAsset.RecipeId.HasValue)
        {
            var recipe = await _dbContext.Recipes.FirstOrDefaultAsync(
                item => item.Id == mediaAsset.RecipeId.Value && !item.IsDeleted,
                cancellationToken);

            if (recipe?.ImageUrl == mediaAsset.SecureUrl)
            {
                recipe.ImageUrl = null;
                recipe.UpdatedAt = DateTime.UtcNow;
            }
        }
    }

    private sealed record CloudinarySettings(string CloudName, string ApiKey, string ApiSecret);
}
