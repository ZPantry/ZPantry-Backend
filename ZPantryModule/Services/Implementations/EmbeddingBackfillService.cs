using AuthenticationModule.DTOs;
using AuthenticationModule.Repositories.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZPantryModule.Services.Interfaces;

namespace ZPantryModule.Services.Implementations;

public sealed class EmbeddingBackfillService : IEmbeddingBackfillService
{
    private const int ExpectedDimension = 1536;
    private const int BatchSize = 25;

    private readonly ZpantryDbContext _dbContext;
    private readonly IAIRecommendationClient _aiRecommendationClient;
    private readonly ILogger<EmbeddingBackfillService> _logger;

    public EmbeddingBackfillService(
        ZpantryDbContext dbContext,
        IAIRecommendationClient aiRecommendationClient,
        ILogger<EmbeddingBackfillService> logger)
    {
        _dbContext = dbContext;
        _aiRecommendationClient = aiRecommendationClient;
        _logger = logger;
    }

    public async Task<EmbeddingBackfillResult> ReembedExistingDataAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting embedding backfill for ingredients and recipes.");

        await EnsureEmbeddingSchemaAsync(cancellationToken);

        var ingredientResult = await ReembedIngredientsAsync(cancellationToken);
        var recipeResult = await ReembedRecipesAsync(cancellationToken);

        _logger.LogInformation(
            "Embedding backfill finished. Ingredients updated: {IngredientUpdates}, recipes updated: {RecipeUpdates}, failed: {FailedCount}.",
            ingredientResult.UpdatedCount,
            recipeResult.UpdatedCount,
            ingredientResult.FailedCount + recipeResult.FailedCount);

        return new EmbeddingBackfillResult(
            ingredientResult.UpdatedCount,
            recipeResult.UpdatedCount,
            ingredientResult.FailedCount + recipeResult.FailedCount);
    }

    private async Task EnsureEmbeddingSchemaAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }

    private async Task<EmbeddingBatchResult> ReembedIngredientsAsync(CancellationToken cancellationToken)
    {
        var updatedCount = 0;
        var failedCount = 0;

        var query = _dbContext.Ingredients
            .Where(ingredient => !ingredient.IsDeleted)
            .OrderBy(ingredient => ingredient.Id);

        var totalCount = await query.CountAsync(cancellationToken);

        for (var offset = 0; offset < totalCount; offset += BatchSize)
        {
            var batch = await query
                .Skip(offset)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);

            foreach (var ingredient in batch)
            {
                var response = await _aiRecommendationClient.EmbedIngredientAsync(
                    new EmbedIngredientAiRequest
                    {
                        IngredientId = ingredient.Id,
                        Name = ingredient.Name,
                        NormalizedName = ingredient.NormalizedName,
                        Category = ingredient.Category
                    },
                    cancellationToken);

                if (!TryApplyEmbedding(
                        response.Success,
                        response.Data?.Embedding,
                        ingredient.Name,
                        ingredient.Id,
                        out var embedding))
                {
                    failedCount++;
                    continue;
                }

                ingredient.Embedding = embedding;
                ingredient.UpdatedAt = DateTime.UtcNow;
                updatedCount++;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            _dbContext.ChangeTracker.Clear();
        }

        return new EmbeddingBatchResult(updatedCount, failedCount);
    }

    private async Task<EmbeddingBatchResult> ReembedRecipesAsync(CancellationToken cancellationToken)
    {
        var updatedCount = 0;
        var failedCount = 0;

        var ingredientNamesByRecipeId = await (
            from recipeIngredient in _dbContext.RecipeIngredients.AsNoTracking()
            join ingredient in _dbContext.Ingredients.AsNoTracking()
                on recipeIngredient.IngredientId equals ingredient.Id
            where !recipeIngredient.IsDeleted && !ingredient.IsDeleted
            select new
            {
                recipeIngredient.RecipeId,
                IngredientName = ingredient.Name
            })
            .ToListAsync(cancellationToken);

        var recipeIngredientLookup = ingredientNamesByRecipeId
            .GroupBy(item => item.RecipeId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(item => item.IngredientName).Distinct().ToList());

        var query = _dbContext.Recipes
            .Where(recipe => !recipe.IsDeleted)
            .OrderBy(recipe => recipe.Id);

        var totalCount = await query.CountAsync(cancellationToken);

        for (var offset = 0; offset < totalCount; offset += BatchSize)
        {
            var batch = await query
                .Skip(offset)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);

            foreach (var recipe in batch)
            {
                recipeIngredientLookup.TryGetValue(recipe.Id, out var ingredientNames);
                ingredientNames ??= [];

                var response = await _aiRecommendationClient.EmbedRecipeAsync(
                    new EmbedRecipeAiRequest
                    {
                        RecipeId = recipe.Id,
                        Name = recipe.Name,
                        Description = recipe.Description,
                        IngredientNames = ingredientNames,
                        InstructionText = recipe.InstructionText
                    },
                    cancellationToken);

                if (!TryApplyEmbedding(
                        response.Success,
                        response.Data?.Embedding,
                        recipe.Name,
                        recipe.Id,
                        out var embedding))
                {
                    failedCount++;
                    continue;
                }

                recipe.Embedding = embedding;
                recipe.UpdatedAt = DateTime.UtcNow;
                updatedCount++;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            _dbContext.ChangeTracker.Clear();
        }

        return new EmbeddingBatchResult(updatedCount, failedCount);
    }

    private bool TryApplyEmbedding(
        bool success,
        IReadOnlyCollection<float>? values,
        string entityName,
        Guid entityId,
        out float[] embedding)
    {
        embedding = [];

        if (!success || values is null || values.Count != ExpectedDimension)
        {
            _logger.LogWarning(
                "Skipping embedding backfill for {EntityName} ({EntityId}) because AI service returned invalid vector length {Length}.",
                entityName,
                entityId,
                values?.Count ?? 0);
            return false;
        }

        embedding = values.ToArray();
        return true;
    }

    private sealed record EmbeddingBatchResult(int UpdatedCount, int FailedCount);
}
