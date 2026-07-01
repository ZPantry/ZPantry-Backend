using AuthenticationModule.DTOs;
using AuthenticationModule.Repositories.Entities;

namespace ZPantryModule.Services.Implementations;

internal static class ZPantryMappings
{
    public static IngredientDto ToDto(this Ingredient ingredient)
        => new()
        {
            Id = ingredient.Id,
            Name = ingredient.Name,
            NormalizedName = ingredient.NormalizedName,
            Category = ingredient.Category,
            Unit = ingredient.Unit,
            CaloriesPerUnit = ingredient.CaloriesPerUnit,
            ProteinPerUnit = ingredient.ProteinPerUnit,
            FatPerUnit = ingredient.FatPerUnit,
            CarbPerUnit = ingredient.CarbPerUnit,
            ImageUrl = ingredient.ImageUrl
        };

    public static RecipeDto ToDto(this Recipe recipe)
        => new()
        {
            Id = recipe.Id,
            Name = recipe.Name,
            Description = recipe.Description,
            CookingTimeMinutes = recipe.CookingTimeMinutes,
            Difficulty = recipe.Difficulty,
            ServingSize = recipe.ServingSize,
            InstructionText = recipe.InstructionText,
            ImageUrl = recipe.ImageUrl,
            SourceType = recipe.SourceType
        };

    public static PantryItemDto ToDto(this UserPantryItem pantryItem)
        => new()
        {
            Id = pantryItem.Id,
            IngredientId = pantryItem.IngredientId,
            Quantity = pantryItem.Quantity,
            Unit = pantryItem.Unit,
            ExpiredAt = pantryItem.ExpiredAt,
            StorageLocation = pantryItem.StorageLocation,
            Note = pantryItem.Note
        };

    public static string NormalizeName(string value)
        => value.Trim().ToLowerInvariant();

    public static (int PageIndex, int PageSize) NormalizePaging(int pageIndex, int pageSize)
        => (Math.Max(pageIndex, 1), Math.Clamp(pageSize, 1, 100));

    public static void SoftDelete(this BaseEntity entity)
    {
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
    }
}
