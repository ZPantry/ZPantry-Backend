namespace AuthenticationModule.Repositories.Entities;

public partial class Recipe : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int? CookingTimeMinutes { get; set; }

    public string? Difficulty { get; set; }

    public int? ServingSize { get; set; }

    public string? InstructionText { get; set; }

    public string? ImageUrl { get; set; }

    public string? SourceType { get; set; }

    public float[]? Embedding { get; set; }
}

