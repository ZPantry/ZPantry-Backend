namespace AuthenticationModule.Repositories.Entities;

public partial class RecommendationFeedback : BaseEntity
{
    public Guid UserId { get; set; }

    public Guid MealRecommendationId { get; set; }

    public Guid RecipeId { get; set; }

    public int? Rating { get; set; }

    public string? FeedbackType { get; set; }

    public string? Comment { get; set; }
}

