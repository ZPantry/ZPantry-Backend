using System;
using System.Collections.Generic;

namespace CuisineModule.Repositories.Entities;

public partial class Ingredient
{
    public Guid IngredientId { get; set; }

    public Guid UserId { get; set; }

    public string IngredientName { get; set; } = null!;

    public decimal Quantity { get; set; }

    public string? Unit { get; set; }

    public string? Reservation { get; set; }

    public DateOnly? ExpiredDate { get; set; }

    public string? Note { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
