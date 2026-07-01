using Microsoft.AspNetCore.Http;

namespace ZPantryModule.DTOs;

public class UploadMediaRequest
{
    public IFormFile File { get; set; } = default!;

    public Guid? IngredientId { get; set; }

    public Guid? RecipeId { get; set; }
}
