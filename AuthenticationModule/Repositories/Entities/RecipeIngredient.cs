namespace AuthenticationModule.Repositories.Entities;

public partial class RecipeIngredient : BaseEntity
{
    public Guid RecipeId { get; set; }

    public Guid IngredientId { get; set; }

    public decimal? Quantity { get; set; }

    public string? Unit { get; set; }

    public bool IsRequired { get; set; }

    public string? Note { get; set; }
}

