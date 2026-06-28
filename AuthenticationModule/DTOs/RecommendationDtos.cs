namespace AuthenticationModule.DTOs;

public class RecommendMealRequest
{
    public Guid UserId { get; set; }

    public string InputIngredientText { get; set; } = string.Empty;

    public List<string> Ingredients { get; set; } = [];

    public List<string> CandidateRecipes { get; set; } = [];

    public int TopK { get; set; } = 5;
}

public class RecommendMealResponse
{
    public List<RecommendMealResponseItem> Items { get; set; } = [];
}

public class RecommendMealResponseItem
{
    public Guid RecipeId { get; set; }

    public string RecipeName { get; set; } = string.Empty;

    public decimal MatchScore { get; set; }

    public int MissingIngredientCount { get; set; }

    public List<string> MissingIngredientNames { get; set; } = [];

    public string Reason { get; set; } = string.Empty;

    public int Rank { get; set; }
}

public class RecommendationFeedbackRequest
{
    public Guid UserId { get; set; }

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

