public class IngredientRequest
{
    public Guid UserId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public string? Reservation { get; set; }
    public DateOnly? ExpiredDate { get; set; }
    public string? Note { get; set; }
}