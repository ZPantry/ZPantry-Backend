namespace AuthenticationModule.Repositories.Entities;

public partial class MealRecommendationItem : BaseEntity
{
    public Guid MealRecommendationId { get; set; }

    public Guid RecipeId { get; set; }

    public decimal? MatchScore { get; set; }

    public int MissingIngredientCount { get; set; }

    public string? MissingIngredientNames { get; set; }

    public string? Reason { get; set; }

    public int Rank { get; set; }
}

