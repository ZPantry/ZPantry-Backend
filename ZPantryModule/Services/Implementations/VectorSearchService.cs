using ZPantryModule.Services.Interfaces;

namespace ZPantryModule.Services.Implementations;

public class VectorSearchService : IVectorSearchService
{
    public Task<IReadOnlyList<object>> FindSimilarRecipesAsync(float[] embedding, int topK, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<object>>([]);

    public Task<IReadOnlyList<object>> FindSimilarIngredientsAsync(float[] embedding, int topK, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<object>>([]);
}

