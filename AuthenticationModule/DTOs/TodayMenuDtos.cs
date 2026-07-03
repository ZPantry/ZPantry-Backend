using Microsoft.AspNetCore.Http;

namespace AuthenticationModule.DTOs;

public class CreateTodayMenuItemRequest
{
    public Guid? MealId { get; set; }

    public Guid? RecipeId { get; set; }

    public string MealName { get; set; } = string.Empty;

    public string? MealType { get; set; }

    public int? ServingSize { get; set; }

    public DateOnly? PlannedDate { get; set; }

    public string? Note { get; set; }
}

public class CompleteTodayMenuItemRequest
{
    public IFormFile? ImageFile { get; set; }

    public DateTime? CookedAt { get; set; }

    public int? Rating { get; set; }

    public string? Note { get; set; }
}

public class TodayMenuItemDto
{
    public Guid Id { get; set; }

    public Guid? MealId { get; set; }

    public Guid? RecipeId { get; set; }

    public string MealName { get; set; } = string.Empty;

    public string? MealType { get; set; }

    public int? ServingSize { get; set; }

    public DateOnly PlannedDate { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? Note { get; set; }

    public DateTime? CookedAt { get; set; }

    public string? ImageUrl { get; set; }

    public string? ImagePublicId { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class TodayMenuIngredientDto
{
    public Guid IngredientId { get; set; }

    public string IngredientName { get; set; } = string.Empty;

    public decimal? Quantity { get; set; }

    public string? Unit { get; set; }

    public bool IsRequired { get; set; } = true;
}

public class PantryUsageLogDto
{
    public Guid Id { get; set; }

    public Guid IngredientId { get; set; }

    public string IngredientName { get; set; } = string.Empty;

    public decimal? QuantityUsed { get; set; }

    public string? Unit { get; set; }

    public string ActionType { get; set; } = string.Empty;

    public string? Warning { get; set; }
}

public class CookingLogDto
{
    public Guid Id { get; set; }

    public Guid TodayMenuItemId { get; set; }

    public Guid? MealId { get; set; }

    public Guid? RecipeId { get; set; }

    public string MealName { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public string? ImagePublicId { get; set; }

    public DateTime CookedAt { get; set; }

    public int? Rating { get; set; }

    public string? Note { get; set; }

    public List<PantryUsageLogDto> PantryUsageLogs { get; set; } = [];
}

public class TodayMenuCompletionResponse
{
    public CookingLogDto CookingLog { get; set; } = new();

    public List<PantryUsageLogDto> ConsumedIngredients { get; set; } = [];

    public List<PantryItemDto> UpdatedPantryItems { get; set; } = [];

    public List<string> Warnings { get; set; } = [];
}

public class TodayMenuItemDetailDto : TodayMenuItemDto
{
    public RecipeDto? Recipe { get; set; }

    public List<TodayMenuIngredientDto> RequiredIngredients { get; set; } = [];

    public List<PantryItemDto> PantryItems { get; set; } = [];

    public List<PantryUsageLogDto> PantryUsageLogs { get; set; } = [];
}

public class MediaUploadResultDto
{
    public Guid? MediaAssetId { get; set; }

    public string PublicId { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string SecureUrl { get; set; } = string.Empty;

    public string? ResourceType { get; set; }

    public string? Format { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }
}
