using System.Net.Http.Json;
using System.Text.Json;
using AuthenticationModule.Contracts.Common;
using AuthenticationModule.DTOs;
using ZPantryModule.Services.Interfaces;

namespace ZPantryModule.Services.Implementations;

public class AIRecommendationClient : IAIRecommendationClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public AIRecommendationClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<ApiResponse<RecommendMealAiResponse>> RecommendMealsAsync(
        RecommendMealAiRequest request,
        CancellationToken cancellationToken = default)
        => PostAsync<RecommendMealAiRequest, RecommendMealAiResponse>(
            "ai/recommend-meals",
            request,
            cancellationToken);

    public Task<ApiResponse<MissingIngredientAiResponse>> SuggestMissingIngredientsAsync(
        MissingIngredientAiRequest request,
        CancellationToken cancellationToken = default)
        => PostAsync<MissingIngredientAiRequest, MissingIngredientAiResponse>(
            "ai/suggest-missing-ingredients",
            request,
            cancellationToken);

    public Task<ApiResponse<EmbeddingAiResponse>> EmbedIngredientAsync(
        EmbedIngredientAiRequest request,
        CancellationToken cancellationToken = default)
        => PostAsync<EmbedIngredientAiRequest, EmbeddingAiResponse>(
            "ai/embed-ingredient",
            request,
            cancellationToken);

    public Task<ApiResponse<EmbeddingAiResponse>> EmbedRecipeAsync(
        EmbedRecipeAiRequest request,
        CancellationToken cancellationToken = default)
        => PostAsync<EmbedRecipeAiRequest, EmbeddingAiResponse>(
            "ai/embed-recipe",
            request,
            cancellationToken);

    private async Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(
        string path,
        TRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync(path, request, JsonOptions, cancellationToken);
            var payload = await response.Content.ReadFromJsonAsync<ApiResponse<TResponse>>(JsonOptions, cancellationToken);

            if (payload is null)
            {
                return ApiResponse<TResponse>.Fail("AI service returned an empty response.");
            }

            if (!response.IsSuccessStatusCode && payload.Success)
            {
                return ApiResponse<TResponse>.Fail($"AI service returned HTTP {(int)response.StatusCode}.");
            }

            return payload;
        }
        catch (TaskCanceledException)
        {
            return ApiResponse<TResponse>.Fail("AI service request timed out.");
        }
        catch (HttpRequestException ex)
        {
            return ApiResponse<TResponse>.Fail($"AI service unavailable: {ex.Message}");
        }
        catch (JsonException ex)
        {
            return ApiResponse<TResponse>.Fail($"AI service response could not be parsed: {ex.Message}");
        }
    }
}
