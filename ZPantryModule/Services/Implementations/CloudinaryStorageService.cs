using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AuthenticationModule.Contracts.Common;
using AuthenticationModule.Repositories.Entities;
using Microsoft.EntityFrameworkCore;
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

    public async Task<ApiResponse<string>> UploadAsync(
        Stream fileStream,
        string fileName,
        Guid? ingredientId = null,
        Guid? recipeId = null,
        CancellationToken cancellationToken = default)
    {
        var settings = GetSettings();
        if (settings is null)
        {
            return ApiResponse<string>.Fail("Cloudinary configuration is missing.");
        }

        if (ingredientId.HasValue && recipeId.HasValue)
        {
            return ApiResponse<string>.Fail("Upload can be linked to either an ingredient or a recipe, not both.");
        }

        if (fileStream.CanSeek && fileStream.Length == 0)
        {
            return ApiResponse<string>.Fail("Uploaded file is empty.");
        }

        var linkedIngredient = ingredientId.HasValue
            ? await _dbContext.Ingredients.FirstOrDefaultAsync(
                ingredient => ingredient.Id == ingredientId.Value && !ingredient.IsDeleted,
                cancellationToken)
            : null;

        if (ingredientId.HasValue && linkedIngredient is null)
        {
            return ApiResponse<string>.Fail("Ingredient not found.");
        }

        var linkedRecipe = recipeId.HasValue
            ? await _dbContext.Recipes.FirstOrDefaultAsync(
                recipe => recipe.Id == recipeId.Value && !recipe.IsDeleted,
                cancellationToken)
            : null;

        if (recipeId.HasValue && linkedRecipe is null)
        {
            return ApiResponse<string>.Fail("Recipe not found.");
        }

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        const string folder = "zpantry";
        var signature = CreateSignature(
            new Dictionary<string, string>
            {
                ["folder"] = folder,
                ["timestamp"] = timestamp
            },
            settings.ApiSecret);

        using var content = new MultipartFormDataContent
        {
            { new StringContent(settings.ApiKey), "api_key" },
            { new StringContent(timestamp), "timestamp" },
            { new StringContent(folder), "folder" },
            { new StringContent(signature), "signature" }
        };

        content.Add(new StreamContent(fileStream), "file", fileName);

        var client = _httpClientFactory.CreateClient();
        using var response = await client.PostAsync(
            $"https://api.cloudinary.com/v1_1/{settings.CloudName}/image/upload",
            content,
            cancellationToken);

        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return ApiResponse<string>.Fail($"Cloudinary upload failed: {responseText}");
        }

        var uploadResult = ParseUploadResult(responseText);
        if (string.IsNullOrWhiteSpace(uploadResult.SecureUrl))
        {
            return ApiResponse<string>.Fail("Cloudinary upload response did not contain a secure URL.");
        }

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

        return ApiResponse<string>.SuccessResponse(uploadResult.SecureUrl, "Media uploaded.");
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

        var mediaAsset = Guid.TryParse(publicId, out var mediaAssetId)
            ? await _dbContext.MediaAssets.FirstOrDefaultAsync(
                asset => asset.Id == mediaAssetId && !asset.IsDeleted,
                cancellationToken)
            : await _dbContext.MediaAssets.FirstOrDefaultAsync(
                asset => asset.PublicId == publicId && !asset.IsDeleted,
                cancellationToken);

        var cloudinaryPublicId = mediaAsset?.PublicId ?? publicId;
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var signature = CreateSignature(
            new Dictionary<string, string>
            {
                ["public_id"] = cloudinaryPublicId,
                ["timestamp"] = timestamp
            },
            settings.ApiSecret);

        using var content = new MultipartFormDataContent
        {
            { new StringContent(settings.ApiKey), "api_key" },
            { new StringContent(timestamp), "timestamp" },
            { new StringContent(cloudinaryPublicId), "public_id" },
            { new StringContent(signature), "signature" }
        };

        var client = _httpClientFactory.CreateClient();
        using var response = await client.PostAsync(
            $"https://api.cloudinary.com/v1_1/{settings.CloudName}/image/destroy",
            content,
            cancellationToken);

        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return ApiResponse<object>.Fail($"Cloudinary delete failed: {responseText}");
        }

        if (mediaAsset is not null)
        {
            await ClearLinkedImageAsync(mediaAsset, cancellationToken);
            mediaAsset.SoftDelete();
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return ApiResponse<object>.SuccessResponse(null, "Media deleted.");
    }

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

    private CloudinarySettings? GetSettings()
    {
        var cloudName = _configuration["CLOUDINARY_CLOUD_NAME"];
        var apiKey = _configuration["CLOUDINARY_API_KEY"];
        var apiSecret = _configuration["CLOUDINARY_API_SECRET"];

        if (string.IsNullOrWhiteSpace(cloudName)
            || string.IsNullOrWhiteSpace(apiKey)
            || string.IsNullOrWhiteSpace(apiSecret))
        {
            return null;
        }

        return new CloudinarySettings(cloudName, apiKey, apiSecret);
    }

    private static string CreateSignature(IReadOnlyDictionary<string, string> parameters, string apiSecret)
    {
        var payload = string.Join("&", parameters
            .OrderBy(parameter => parameter.Key, StringComparer.Ordinal)
            .Select(parameter => $"{parameter.Key}={parameter.Value}"));

        var bytes = SHA1.HashData(Encoding.UTF8.GetBytes(payload + apiSecret));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static CloudinaryUploadResult ParseUploadResult(string responseText)
    {
        using var document = JsonDocument.Parse(responseText);
        var root = document.RootElement;

        return new CloudinaryUploadResult(
            GetString(root, "public_id"),
            GetString(root, "url"),
            GetString(root, "secure_url"),
            GetString(root, "resource_type"),
            GetString(root, "format"),
            GetInt(root, "width"),
            GetInt(root, "height"));
    }

    private static string GetString(JsonElement root, string propertyName)
        => root.TryGetProperty(propertyName, out var property)
            ? property.GetString() ?? string.Empty
            : string.Empty;

    private static int? GetInt(JsonElement root, string propertyName)
        => root.TryGetProperty(propertyName, out var property) && property.TryGetInt32(out var value)
            ? value
            : null;

    private sealed record CloudinarySettings(string CloudName, string ApiKey, string ApiSecret);

    private sealed record CloudinaryUploadResult(
        string PublicId,
        string Url,
        string SecureUrl,
        string ResourceType,
        string Format,
        int? Width,
        int? Height);
}
