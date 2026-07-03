using AuthenticationModule.DTOs;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace ZPantryModule.DTOs;

public class CreateIngredientFormRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Category { get; set; }

    public string? Unit { get; set; }

    public decimal? CaloriesPerUnit { get; set; }

    public decimal? ProteinPerUnit { get; set; }

    public decimal? FatPerUnit { get; set; }

    public decimal? CarbPerUnit { get; set; }

    public string? GradientFrom { get; set; }

    public string? GradientTo { get; set; }

    public string? ImageUrl { get; set; }

    public IFormFile? ImageFile { get; set; }
}

public class UpdateIngredientFormRequest
{
    public string? Name { get; set; }

    public string? Category { get; set; }

    public string? Unit { get; set; }

    public decimal? CaloriesPerUnit { get; set; }

    public decimal? ProteinPerUnit { get; set; }

    public decimal? FatPerUnit { get; set; }

    public decimal? CarbPerUnit { get; set; }

    public string? GradientFrom { get; set; }

    public string? GradientTo { get; set; }

    public string? ImageUrl { get; set; }

    public IFormFile? ImageFile { get; set; }
}

public class CreateRecipeFormRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int? CookingTimeMinutes { get; set; }

    public string? Difficulty { get; set; }

    public int? ServingSize { get; set; }

    public string? InstructionText { get; set; }

    public string? SourceType { get; set; }

    public string? GradientFrom { get; set; }

    public string? GradientTo { get; set; }

    public string? ImageUrl { get; set; }

    public string? IngredientsJson { get; set; }

    public IFormFile? ImageFile { get; set; }

    public bool TryParseIngredients(out List<RecipeIngredientDto> ingredients)
    {
        if (string.IsNullOrWhiteSpace(IngredientsJson))
        {
            ingredients = [];
            return true;
        }

        try
        {
            ingredients = JsonSerializer.Deserialize<List<RecipeIngredientDto>>(
                IngredientsJson,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? [];

            return true;
        }
        catch (JsonException)
        {
            ingredients = [];
            return false;
        }
    }
}

public class UpdateRecipeFormRequest
{
    public string? Name { get; set; }

    public string? Description { get; set; }

    public int? CookingTimeMinutes { get; set; }

    public string? Difficulty { get; set; }

    public int? ServingSize { get; set; }

    public string? InstructionText { get; set; }

    public string? SourceType { get; set; }

    public string? GradientFrom { get; set; }

    public string? GradientTo { get; set; }

    public string? ImageUrl { get; set; }

    public string? IngredientsJson { get; set; }

    public IFormFile? ImageFile { get; set; }

    public bool TryParseIngredients(out List<RecipeIngredientDto> ingredients)
    {
        if (string.IsNullOrWhiteSpace(IngredientsJson))
        {
            ingredients = [];
            return true;
        }

        try
        {
            ingredients = JsonSerializer.Deserialize<List<RecipeIngredientDto>>(
                IngredientsJson,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? [];

            return true;
        }
        catch (JsonException)
        {
            ingredients = [];
            return false;
        }
    }
}
