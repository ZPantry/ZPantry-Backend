namespace AuthenticationModule.Repositories.Entities;

public partial class CookingLog : BaseEntity
{
    public Guid UserId { get; set; }

    public Guid TodayMenuItemId { get; set; }

    public Guid? MealId { get; set; }

    public Guid? RecipeId { get; set; }

    public string MealName { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public string? ImagePublicId { get; set; }

    public DateTime CookedAt { get; set; }

    public int? Rating { get; set; }

    public string? Note { get; set; }
}
