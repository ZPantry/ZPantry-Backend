using CuisineModule.Repositories.Entities;

public interface IIngredientService
{
    Task<List<Ingredient>> GetAllIngredientsAsync();
    Task<List<Ingredient>> GetIngredientsByUserId(Guid userId);
    Task AddIngredient(IngredientRequest request);
    Task UpdateIngredient(Guid ingredientId, IngredientRequest request);
    Task DeleteIngredient(Guid ingredientId);
}