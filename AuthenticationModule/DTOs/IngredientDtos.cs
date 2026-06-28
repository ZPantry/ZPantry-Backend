namespace AuthenticationModule.DTOs;

public class IngredientDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string NormalizedName { get; set; } = string.Empty;

    public string? Category { get; set; }

    public string? Unit { get; set; }

    public decimal? CaloriesPerUnit { get; set; }

    public decimal? ProteinPerUnit { get; set; }

    public decimal? FatPerUnit { get; set; }

    public decimal? CarbPerUnit { get; set; }

    public string? ImageUrl { get; set; }
}

public class CreateIngredientRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Category { get; set; }

    public string? Unit { get; set; }

    public decimal? CaloriesPerUnit { get; set; }

    public decimal? ProteinPerUnit { get; set; }

    public decimal? FatPerUnit { get; set; }

    public decimal? CarbPerUnit { get; set; }

    public string? ImageUrl { get; set; }
}

public class UpdateIngredientRequest : CreateIngredientRequest
{
}

