using AuthenticationModule.Repositories.Entities;

namespace AuthenticationModule.Repositories.Interfaces;

public interface IIngredientRepository
{
    Task<List<Ingredient>> GetPagedAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<Ingredient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Ingredient entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(Ingredient entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Ingredient entity, CancellationToken cancellationToken = default);
}

public interface IRecipeRepository
{
    Task<List<Recipe>> GetPagedAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<Recipe?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Recipe entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(Recipe entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Recipe entity, CancellationToken cancellationToken = default);
}

public interface IUserPantryRepository
{
    Task<List<UserPantryItem>> GetByUserIdAsync(Guid userId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
    Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserPantryItem?> GetByIdAsync(Guid userId, Guid itemId, CancellationToken cancellationToken = default);
    Task AddAsync(UserPantryItem entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserPantryItem entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(UserPantryItem entity, CancellationToken cancellationToken = default);
}

public interface IRecommendationRepository
{
    Task<List<MealRecommendation>> GetPagedAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<MealRecommendation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(MealRecommendation entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(MealRecommendation entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(MealRecommendation entity, CancellationToken cancellationToken = default);
}

public interface IMediaRepository
{
    Task<List<MediaAsset>> GetByOwnerIdAsync(Guid? recipeId, Guid? ingredientId, CancellationToken cancellationToken = default);
    Task<MediaAsset?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(MediaAsset entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(MediaAsset entity, CancellationToken cancellationToken = default);
}

