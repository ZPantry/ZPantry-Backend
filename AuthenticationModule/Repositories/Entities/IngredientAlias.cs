namespace AuthenticationModule.Repositories.Entities;

public partial class IngredientAlias : BaseEntity
{
    public Guid IngredientId { get; set; }

    public string AliasName { get; set; } = string.Empty;

    public string NormalizedAliasName { get; set; } = string.Empty;
}

