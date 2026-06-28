namespace AuthenticationModule.Repositories.Entities;

public partial class MealRecommendation : BaseEntity
{
    public Guid UserId { get; set; }

    public string? RequestText { get; set; }

    public string? InputIngredientText { get; set; }

    public string? RecommendationType { get; set; }

    public string? Status { get; set; }

    public DateTime? CompletedAt { get; set; }
}

