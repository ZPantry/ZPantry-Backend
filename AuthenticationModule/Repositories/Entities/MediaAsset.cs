namespace AuthenticationModule.Repositories.Entities;

public partial class MediaAsset : BaseEntity
{
    public Guid? RecipeId { get; set; }

    public Guid? IngredientId { get; set; }

    public string PublicId { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string SecureUrl { get; set; } = string.Empty;

    public string? ResourceType { get; set; }

    public string? Format { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }
}

