namespace AuthenticationModule.DTOs;

public class RecipeDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int? CookingTimeMinutes { get; set; }

    public string? Difficulty { get; set; }

    public int? ServingSize { get; set; }

    public string? InstructionText { get; set; }

    public string? ImageUrl { get; set; }

    public string? SourceType { get; set; }
}

public class RecipeIngredientDto
{
    public Guid IngredientId { get; set; }

    public decimal? Quantity { get; set; }

    public string? Unit { get; set; }

    public bool IsRequired { get; set; }

    public string? Note { get; set; }
}

public class CreateRecipeRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int? CookingTimeMinutes { get; set; }

    public string? Difficulty { get; set; }

    public int? ServingSize { get; set; }

    public string? InstructionText { get; set; }

    public string? ImageUrl { get; set; }

    public string? SourceType { get; set; }

    public List<RecipeIngredientDto> Ingredients { get; set; } = [];
}

public class UpdateRecipeRequest : CreateRecipeRequest
{
}

