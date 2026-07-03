namespace AuthenticationModule.Repositories.Entities;

public partial class TodayMenuItem : BaseEntity
{
    public Guid UserId { get; set; }

    public Guid? MealId { get; set; }

    public Guid? RecipeId { get; set; }

    public string MealName { get; set; } = string.Empty;

    public string? MealType { get; set; }

    public int? ServingSize { get; set; }

    public DateOnly PlannedDate { get; set; }

    public TodayMenuStatus Status { get; set; } = TodayMenuStatus.Planned;

    public string? Note { get; set; }

    public DateTime? CookedAt { get; set; }

    public string? ImageUrl { get; set; }

    public string? ImagePublicId { get; set; }
}
