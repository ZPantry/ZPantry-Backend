namespace AuthenticationModule.DTOs;

public class RecommendMealAiRequest
{
    public Guid UserId { get; set; }

    public string InputIngredientText { get; set; } = string.Empty;

    public List<AiIngredientItem> Ingredients { get; set; } = [];

    public List<AiCandidateRecipeItem> CandidateRecipes { get; set; } = [];

    public int TopK { get; set; } = 5;
}

public class AiIngredientItem
{
    public Guid? IngredientId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal? Quantity { get; set; }

    public string? Unit { get; set; }
}

public class AiCandidateRecipeItem
{
    public Guid RecipeId { get; set; }

    public string RecipeName { get; set; } = string.Empty;

    public List<string> IngredientNames { get; set; } = [];

    public string? InstructionText { get; set; }
}

public class RecommendMealAiItem
{
    public Guid RecipeId { get; set; }

    public string RecipeName { get; set; } = string.Empty;

    public decimal MatchScore { get; set; }

    public int MissingIngredientCount { get; set; }

    public List<string> MissingIngredientNames { get; set; } = [];

    public string Reason { get; set; } = string.Empty;

    public int Rank { get; set; }
}

public class RecommendMealAiResponse
{
    public List<RecommendMealAiItem> Items { get; set; } = [];
}

public class MissingIngredientAiRequest
{
    public Guid RecipeId { get; set; }

    public string RecipeName { get; set; } = string.Empty;

    public List<string> RequiredIngredients { get; set; } = [];

    public List<string> UserIngredients { get; set; } = [];
}

public class MissingIngredientAiResponse
{
    public Guid RecipeId { get; set; }

    public List<string> MissingIngredients { get; set; } = [];
}

public class EmbedIngredientAiRequest
{
    public Guid IngredientId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string NormalizedName { get; set; } = string.Empty;

    public string? Category { get; set; }
}

public class EmbedRecipeAiRequest
{
    public Guid RecipeId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public List<string> IngredientNames { get; set; } = [];

    public string? InstructionText { get; set; }
}

public class EmbeddingAiResponse
{
    public Guid? IngredientId { get; set; }

    public Guid? RecipeId { get; set; }

    public List<float> Embedding { get; set; } = [];

    public int Dimension { get; set; }
}

