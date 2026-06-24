using CuisineModule.Repositories.Entities;
using System.Linq;
using Microsoft.EntityFrameworkCore;

public class IngredientRepository : IIngredientRepository
{
    private readonly ZpantryDbContext _context;

    public IngredientRepository(ZpantryDbContext context)
    {
        _context = context;
    }

    public async Task<List<Ingredient>> GetAllIngredientsAsync()
    {
        return await _context.Ingredients.ToListAsync();
    }

    public async Task<List<Ingredient>> GetIngredientsByUserId(Guid userId)
    {
        return await _context.Ingredients.Where(x => x.UserId == userId).ToListAsync();
    }

    public async Task<Ingredient?> GetIngredientByIngredientId(Guid ingredientId)
    {
        return await _context.Ingredients.FirstOrDefaultAsync(x => x.IngredientId == ingredientId);
    }

    public async Task AddIngredient(Ingredient ingredient)
    {
        _context.Ingredients.Add(ingredient);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateIngredient(Ingredient ingredient)
    {
        _context.Ingredients.Update(ingredient);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteIngredient(Ingredient ingredient)
    {
        _context.Ingredients.Remove(ingredient);
        await _context.SaveChangesAsync();
    }
}