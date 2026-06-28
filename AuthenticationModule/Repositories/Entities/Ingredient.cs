namespace AuthenticationModule.Repositories.Entities;

public partial class Ingredient : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string NormalizedName { get; set; } = string.Empty;

    public string? Category { get; set; }

    public string? Unit { get; set; }

    public decimal? CaloriesPerUnit { get; set; }

    public decimal? ProteinPerUnit { get; set; }

    public decimal? FatPerUnit { get; set; }

    public decimal? CarbPerUnit { get; set; }

    public string? ImageUrl { get; set; }

    public float[]? Embedding { get; set; }
}

