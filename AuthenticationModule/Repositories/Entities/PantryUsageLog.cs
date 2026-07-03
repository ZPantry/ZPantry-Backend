namespace AuthenticationModule.Repositories.Entities;

public partial class PantryUsageLog : BaseEntity
{
    public Guid UserId { get; set; }

    public Guid TodayMenuItemId { get; set; }

    public Guid CookingLogId { get; set; }

    public Guid IngredientId { get; set; }

    public string IngredientName { get; set; } = string.Empty;

    public decimal? QuantityUsed { get; set; }

    public string? Unit { get; set; }

    public string ActionType { get; set; } = "consumed";

    public string? Warning { get; set; }
}
