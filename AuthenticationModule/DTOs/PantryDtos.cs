namespace AuthenticationModule.DTOs;

public class PantryItemDto
{
    public Guid Id { get; set; }

    public Guid IngredientId { get; set; }

    public decimal? Quantity { get; set; }

    public string? Unit { get; set; }

    public DateTime? ExpiredAt { get; set; }

    public string? StorageLocation { get; set; }

    public string? Note { get; set; }
}

public class UpsertPantryItemRequest
{
    public Guid IngredientId { get; set; }

    public decimal? Quantity { get; set; }

    public string? Unit { get; set; }

    public DateTime? ExpiredAt { get; set; }

    public string? StorageLocation { get; set; }

    public string? Note { get; set; }
}
