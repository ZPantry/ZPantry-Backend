using CuisineModule.Repositories.Entities;

public class IngredientService : IIngredientService
{
    private readonly IIngredientRepository _ingredientRepository;

    public IngredientService(IIngredientRepository ingredientRepository)
    {
        _ingredientRepository = ingredientRepository;
    }

    public async Task<List<Ingredient>> GetAllIngredientsAsync()
    {
        return await _ingredientRepository.GetAllIngredientsAsync();
    }

    public async Task<List<Ingredient>> GetIngredientsByUserId(Guid userId)
    {
        return await _ingredientRepository.GetIngredientsByUserId(userId);
    }

    public async Task AddIngredient(IngredientRequest request)
    {
        var ingredient = new Ingredient
        {
            UserId = request.UserId,
            IngredientName = request.IngredientName,
            Quantity = request.Quantity,
            Unit = request.Unit,
            Reservation = request.Reservation,
            ExpiredDate = request.ExpiredDate,
            Note = request.Note
        };
        await _ingredientRepository.AddIngredient(ingredient);
    }

    public async Task UpdateIngredient(Guid ingredientId, IngredientRequest request)
    {
        var ingredient = await _ingredientRepository.GetIngredientByIngredientId(ingredientId);

        if (ingredient == null)
        {
            throw new Exception($"Ingredient with ID {ingredientId} not found.");
        }

        if (!string.IsNullOrWhiteSpace(request.IngredientName) && ingredient.IngredientName != request.IngredientName)
        {
            ingredient.IngredientName = request.IngredientName;
        }

        if (request.Quantity > 0 && ingredient.Quantity != request.Quantity)
        {
            ingredient.Quantity = request.Quantity;
        }

        if (!string.IsNullOrWhiteSpace(request.Unit) && ingredient.Unit != request.Unit)
        {
            ingredient.Unit = request.Unit;
        }

        if (!string.IsNullOrWhiteSpace(request.Reservation) && ingredient.Reservation != request.Reservation)
        {
            ingredient.Reservation = request.Reservation;
        }

        if (request.ExpiredDate.HasValue && ingredient.ExpiredDate != request.ExpiredDate)
        {
            ingredient.ExpiredDate = request.ExpiredDate;
        }

        if (!string.IsNullOrWhiteSpace(request.Note) && ingredient.Note != request.Note)
        {
            ingredient.Note = request.Note;
        }

        await _ingredientRepository.UpdateIngredient(ingredient);
    }

    public async Task DeleteIngredient(Guid ingredientId)
    {
        var ingredient = await _ingredientRepository.GetIngredientByIngredientId(ingredientId);
        
        if (ingredient == null)
        {
            throw new Exception($"Ingredient with ID {ingredientId} not found.");
        }
        
        await _ingredientRepository.DeleteIngredient(ingredient);
    }
}