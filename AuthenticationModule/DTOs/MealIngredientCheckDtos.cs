namespace AuthenticationModule.DTOs;

public class MealIngredientCheckResponse
{
    public Guid MealId { get; set; }

    public string MealName { get; set; } = string.Empty;

    public List<MealIngredientCheckItem> AvailableIngredients { get; set; } = [];

    public List<MealIngredientCheckItem> MissingIngredients { get; set; } = [];

    public string? Note { get; set; }
}

public class MealIngredientCheckItem
{
    public Guid IngredientId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal? Quantity { get; set; }

    public string? Unit { get; set; }
}

public class MealIngredientCheckAiRequest
{
    public Guid UserId { get; set; }

    public MealIngredientCheckAiMeal Meal { get; set; } = new();

    public List<MealIngredientCheckAiIngredient> RequiredIngredients { get; set; } = [];

    public List<MealIngredientCheckAiIngredient> FridgeIngredients { get; set; } = [];
}

public class MealIngredientCheckAiMeal
{
    public Guid MealId { get; set; }

    public string MealName { get; set; } = string.Empty;
}

public class MealIngredientCheckAiIngredient
{
    public Guid? IngredientId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal? Quantity { get; set; }

    public string? Unit { get; set; }
}

public class MealIngredientCheckAiResponse
{
    public Guid MealId { get; set; }

    public string MealName { get; set; } = string.Empty;

    public List<MealIngredientCheckAiIngredient> AvailableIngredients { get; set; } = [];

    public List<MealIngredientCheckAiIngredient> MissingIngredients { get; set; } = [];

    public string? Note { get; set; }
}
