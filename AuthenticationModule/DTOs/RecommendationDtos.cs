namespace AuthenticationModule.DTOs;

public class RecommendMealRequest
{
    public string InputIngredientText { get; set; } = string.Empty;

    public List<string> Ingredients { get; set; } = [];

    public List<RecommendMealIngredientRequest> SelectedIngredients { get; set; } = [];

    public List<string> CandidateRecipes { get; set; } = [];

    public int TopK { get; set; } = 5;
}

public class RecommendMealIngredientRequest
{
    public Guid IngredientId { get; set; }

    public string? Name { get; set; }

    public decimal? Quantity { get; set; }

    public string? Unit { get; set; }
}

public class RecommendMealResponse
{
    public List<RecommendMealResponseItem> Items { get; set; } = [];
}

public class RecommendMealResponseItem
{
    public Guid RecipeId { get; set; }

    public string RecipeName { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public decimal MatchScore { get; set; }

    public int MissingIngredientCount { get; set; }

    public List<string> MissingIngredientNames { get; set; } = [];

    public string Reason { get; set; } = string.Empty;

    public int Rank { get; set; }
}

public class RecommendationFeedbackRequest
{
    public Guid MealRecommendationId { get; set; }

    public Guid RecipeId { get; set; }

    public int Rating { get; set; }

    public string? FeedbackType { get; set; }

    public string? Comment { get; set; }
}

public class MissingIngredientSuggestionResponse
{
    public Guid RecipeId { get; set; }

    public List<string> MissingIngredients { get; set; } = [];
}
