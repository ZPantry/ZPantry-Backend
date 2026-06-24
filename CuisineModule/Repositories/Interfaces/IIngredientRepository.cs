using CuisineModule.Repositories.Entities;

public interface IIngredientRepository
{
    Task<List<Ingredient>> GetAllIngredientsAsync();
    Task<List<Ingredient>> GetIngredientsByUserId(Guid userId);
    Task<Ingredient?> GetIngredientByIngredientId(Guid ingredientId);
    Task AddIngredient(Ingredient ingredient);
    Task UpdateIngredient(Ingredient ingredient);
    Task DeleteIngredient(Ingredient ingredient);
}