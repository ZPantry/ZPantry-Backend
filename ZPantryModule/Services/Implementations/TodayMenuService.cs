using AuthenticationModule.Contracts.Common;
using AuthenticationModule.DTOs;
using AuthenticationModule.Repositories.Entities;
using Microsoft.EntityFrameworkCore;
using ZPantryModule.Services.Interfaces;

namespace ZPantryModule.Services.Implementations;

public class TodayMenuService : ITodayMenuService
{
    private readonly ZpantryDbContext _dbContext;
    private readonly ICloudinaryStorageService _cloudinaryStorageService;

    public TodayMenuService(
        ZpantryDbContext dbContext,
        ICloudinaryStorageService cloudinaryStorageService)
    {
        _dbContext = dbContext;
        _cloudinaryStorageService = cloudinaryStorageService;
    }

    public async Task<PagedResponse<TodayMenuItemDto>> GetByUserAndDateAsync(
        Guid userId,
        DateOnly? plannedDate,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var paging = ZPantryMappings.NormalizePaging(pageIndex, pageSize);
        var date = plannedDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var query = _dbContext.TodayMenuItems
            .AsNoTracking()
            .Where(item => item.UserId == userId && !item.IsDeleted && item.PlannedDate == date)
            .OrderByDescending(item => item.CreatedAt);

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((paging.PageIndex - 1) * paging.PageSize)
            .Take(paging.PageSize)
            .Select(item => new TodayMenuItemDto
            {
                Id = item.Id,
                MealId = item.MealId,
                RecipeId = item.RecipeId,
                MealName = item.MealName,
                MealType = item.MealType,
                ServingSize = item.ServingSize,
                PlannedDate = item.PlannedDate,
                Status = item.Status.ToString(),
                Note = item.Note,
                CookedAt = item.CookedAt,
                ImageUrl = item.ImageUrl,
                ImagePublicId = item.ImagePublicId,
                CreatedAt = item.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return PagedResponse<TodayMenuItemDto>.SuccessPage(items, paging.PageIndex, paging.PageSize, totalItems);
    }

    public async Task<ApiResponse<TodayMenuItemDetailDto>> GetByIdAsync(
        Guid userId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.TodayMenuItems
            .AsNoTracking()
            .FirstOrDefaultAsync(menuItem => menuItem.Id == id && menuItem.UserId == userId && !menuItem.IsDeleted, cancellationToken);

        if (item is null)
        {
            return ApiResponse<TodayMenuItemDetailDto>.Fail("Today menu item not found.");
        }

        var recipeId = ResolveRecipeId(item.MealId, item.RecipeId);
        var recipe = recipeId.HasValue
            ? await _dbContext.Recipes
                .AsNoTracking()
                .FirstOrDefaultAsync(recipeItem => recipeItem.Id == recipeId.Value && !recipeItem.IsDeleted, cancellationToken)
            : null;

        var requiredIngredients = recipeId.HasValue
            ? await LoadRecipeIngredientsAsync(recipeId.Value, cancellationToken)
            : [];

        var pantryItems = await LoadUserPantryItemsAsync(userId, cancellationToken);
        var usageLogs = await LoadUsageLogsAsync(id, cancellationToken);

        return ApiResponse<TodayMenuItemDetailDto>.SuccessResponse(
            new TodayMenuItemDetailDto
            {
                Id = item.Id,
                MealId = item.MealId,
                RecipeId = item.RecipeId,
                MealName = item.MealName,
                MealType = item.MealType,
                ServingSize = item.ServingSize,
                PlannedDate = item.PlannedDate,
                Status = item.Status.ToString(),
                Note = item.Note,
                CookedAt = item.CookedAt,
                ImageUrl = item.ImageUrl,
                ImagePublicId = item.ImagePublicId,
                CreatedAt = item.CreatedAt,
                Recipe = recipe is null ? null : recipe.ToDto(),
                RequiredIngredients = requiredIngredients,
                PantryItems = pantryItems,
                PantryUsageLogs = usageLogs
            },
            "Today menu item loaded.");
    }

    public async Task<ApiResponse<TodayMenuItemDto>> CreateAsync(
        Guid userId,
        CreateTodayMenuItemRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.MealId.HasValue && request.RecipeId.HasValue && request.MealId.Value != request.RecipeId.Value)
        {
            return ApiResponse<TodayMenuItemDto>.Fail("MealId and RecipeId must match when both are provided.");
        }

        var recipeId = ResolveRecipeId(request.MealId, request.RecipeId);
        if (!recipeId.HasValue)
        {
            return ApiResponse<TodayMenuItemDto>.Fail("MealId or RecipeId is required.");
        }

        var recipe = await _dbContext.Recipes
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == recipeId.Value && !item.IsDeleted, cancellationToken);

        if (recipe is null)
        {
            return ApiResponse<TodayMenuItemDto>.Fail("Recipe not found.");
        }

        var plannedDate = request.PlannedDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var mealName = string.IsNullOrWhiteSpace(request.MealName) ? recipe.Name : request.MealName.Trim();
        var mealType = string.IsNullOrWhiteSpace(request.MealType) ? null : request.MealType.Trim();

        var duplicateExists = await _dbContext.TodayMenuItems.AnyAsync(
            item => item.UserId == userId
                && !item.IsDeleted
                && item.PlannedDate == plannedDate
                && item.RecipeId == recipeId.Value
                && item.MealType == mealType,
            cancellationToken);

        if (duplicateExists)
        {
            return ApiResponse<TodayMenuItemDto>.Fail("This meal already exists in today's menu.");
        }

        var todayMenuItem = new TodayMenuItem
        {
            UserId = userId,
            MealId = request.MealId ?? recipeId,
            RecipeId = recipeId,
            MealName = mealName,
            MealType = mealType,
            ServingSize = request.ServingSize,
            PlannedDate = plannedDate,
            Status = TodayMenuStatus.Planned,
            Note = request.Note
        };

        _dbContext.TodayMenuItems.Add(todayMenuItem);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponse<TodayMenuItemDto>.SuccessResponse(
            ToDto(todayMenuItem),
            "Meal added to today menu.");
    }

    public async Task<ApiResponse<object>> DeleteAsync(
        Guid userId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.TodayMenuItems.FirstOrDefaultAsync(
            menuItem => menuItem.Id == id && menuItem.UserId == userId && !menuItem.IsDeleted,
            cancellationToken);

        if (item is null)
        {
            return ApiResponse<object>.Fail("Today menu item not found.");
        }

        if (item.Status == TodayMenuStatus.Cooked)
        {
            return ApiResponse<object>.Fail("Cooked menu items cannot be deleted.");
        }

        item.SoftDelete();
        item.Status = TodayMenuStatus.Cancelled;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponse<object>.SuccessResponse(null, "Today menu item deleted.");
    }

    public async Task<ApiResponse<TodayMenuCompletionResponse>> CompleteAsync(
        Guid userId,
        Guid id,
        CompleteTodayMenuItemRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.ImageFile is null)
        {
            return ApiResponse<TodayMenuCompletionResponse>.Fail("ImageFile is required.");
        }

        var todayMenuItem = await _dbContext.TodayMenuItems.FirstOrDefaultAsync(
            item => item.Id == id && item.UserId == userId && !item.IsDeleted,
            cancellationToken);

        if (todayMenuItem is null)
        {
            return ApiResponse<TodayMenuCompletionResponse>.Fail("Today menu item not found.");
        }

        if (todayMenuItem.Status == TodayMenuStatus.Cooked)
        {
            return ApiResponse<TodayMenuCompletionResponse>.Fail("This meal has already been completed.");
        }

        var recipeId = ResolveRecipeId(todayMenuItem.MealId, todayMenuItem.RecipeId);
        if (!recipeId.HasValue)
        {
            return ApiResponse<TodayMenuCompletionResponse>.Fail("This today menu item does not have a resolved recipe.");
        }

        await using var imageStream = request.ImageFile.OpenReadStream();
        var uploadResponse = await _cloudinaryStorageService.UploadDetailedAsync(
            imageStream,
            request.ImageFile.FileName,
            cancellationToken: cancellationToken);

        if (!uploadResponse.Success || uploadResponse.Data is null)
        {
            return ApiResponse<TodayMenuCompletionResponse>.Fail(uploadResponse.Message, uploadResponse.Errors, uploadResponse.TraceId);
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var uploadedImagePublicId = uploadResponse.Data.PublicId;

        try
        {
            todayMenuItem = await _dbContext.TodayMenuItems.FirstAsync(
                item => item.Id == id && item.UserId == userId && !item.IsDeleted,
                cancellationToken);

            if (todayMenuItem.Status == TodayMenuStatus.Cooked)
            {
                return ApiResponse<TodayMenuCompletionResponse>.Fail("This meal has already been completed.");
            }

            var recipe = await _dbContext.Recipes
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == recipeId.Value && !item.IsDeleted, cancellationToken);

            if (recipe is null)
            {
                return ApiResponse<TodayMenuCompletionResponse>.Fail("Recipe not found.");
            }

            var recipeIngredients = await LoadRecipeIngredientsWithMetadataAsync(recipe.Id, cancellationToken);
            var pantryItems = await _dbContext.UserPantryItems
                .Where(item => item.UserId == userId && !item.IsDeleted)
                .OrderBy(item => item.ExpiredAt ?? DateTime.MaxValue)
                .ThenBy(item => item.CreatedAt)
                .ToListAsync(cancellationToken);

            var pantryIngredientNames = await LoadPantryIngredientNamesAsync(pantryItems, cancellationToken);

            var cookingLog = new CookingLog
            {
                UserId = userId,
                TodayMenuItemId = todayMenuItem.Id,
                MealId = todayMenuItem.MealId,
                RecipeId = recipeId,
                MealName = todayMenuItem.MealName,
                ImageUrl = uploadResponse.Data.SecureUrl,
                ImagePublicId = uploadResponse.Data.PublicId,
                CookedAt = request.CookedAt ?? DateTime.UtcNow,
                Rating = request.Rating,
                Note = request.Note ?? todayMenuItem.Note
            };

            _dbContext.CookingLogs.Add(cookingLog);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var warnings = new List<string>();
            var pantryUsageLogs = new List<PantryUsageLog>();
            var updatedPantryItemIds = new HashSet<Guid>();

            foreach (var ingredient in recipeIngredients)
            {
                if (!ingredient.IsRequired)
                {
                    continue;
                }

                var matchedPantryItems = pantryItems
                    .Where(item => IsIngredientMatch(item.IngredientId, pantryIngredientNames, ingredient))
                    .ToList();

                if (matchedPantryItems.Count == 0)
                {
                    warnings.Add($"Missing pantry item for {ingredient.IngredientName}.");
                    continue;
                }

                var requiredQuantity = ingredient.Quantity ?? 0m;
                if (requiredQuantity <= 0)
                {
                    warnings.Add($"Ingredient {ingredient.IngredientName} does not have a usable quantity.");
                    continue;
                }

                var remainingQuantity = requiredQuantity;
                foreach (var pantryItem in matchedPantryItems)
                {
                    if (remainingQuantity <= 0)
                    {
                        break;
                    }

                    if (!IsUnitCompatible(pantryItem.Unit, ingredient.Unit))
                    {
                        warnings.Add($"Unit mismatch for {ingredient.IngredientName}: pantry='{pantryItem.Unit ?? "n/a"}', recipe='{ingredient.Unit ?? "n/a"}'.");
                        continue;
                    }

                    if (!pantryItem.Quantity.HasValue)
                    {
                        warnings.Add($"Pantry quantity is missing for {ingredient.IngredientName}.");
                        continue;
                    }

                    var consumeQuantity = Math.Min(pantryItem.Quantity.Value, remainingQuantity);
                    pantryItem.Quantity = pantryItem.Quantity.Value - consumeQuantity;
                    pantryItem.UpdatedAt = DateTime.UtcNow;
                    updatedPantryItemIds.Add(pantryItem.Id);
                    remainingQuantity -= consumeQuantity;

                    if (pantryItem.Quantity.Value <= 0m)
                    {
                        pantryItem.SoftDelete();
                    }

                    var usageLog = new PantryUsageLog
                    {
                        UserId = userId,
                        TodayMenuItemId = todayMenuItem.Id,
                        CookingLogId = cookingLog.Id,
                        IngredientId = ingredient.IngredientId,
                        IngredientName = ingredient.IngredientName,
                        QuantityUsed = consumeQuantity,
                        Unit = pantryItem.Unit ?? ingredient.Unit,
                        ActionType = "consumed"
                    };

                    pantryUsageLogs.Add(usageLog);
                    _dbContext.PantryUsageLogs.Add(usageLog);
                }

                if (remainingQuantity > 0)
                {
                    warnings.Add($"Not enough pantry quantity for {ingredient.IngredientName}.");
                }
            }

            todayMenuItem.Status = TodayMenuStatus.Cooked;
            todayMenuItem.CookedAt = cookingLog.CookedAt;
            todayMenuItem.ImageUrl = uploadResponse.Data.SecureUrl;
            todayMenuItem.ImagePublicId = uploadResponse.Data.PublicId;
            todayMenuItem.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var updatedPantryItems = await LoadPantryItemsByIdsAsync(userId, updatedPantryItemIds, cancellationToken);

            return ApiResponse<TodayMenuCompletionResponse>.SuccessResponse(
                new TodayMenuCompletionResponse
                {
                    CookingLog = ToCookingLogDto(cookingLog, pantryUsageLogs),
                    ConsumedIngredients = pantryUsageLogs.Select(ToPantryUsageLogDto).ToList(),
                    UpdatedPantryItems = updatedPantryItems,
                    Warnings = warnings
                },
                "Meal completed and cooking log saved.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            await _cloudinaryStorageService.DeleteAsync(uploadedImagePublicId, cancellationToken);
            return ApiResponse<TodayMenuCompletionResponse>.Fail($"Unable to complete today menu item: {ex.Message}");
        }
    }

    public async Task<PagedResponse<CookingLogDto>> GetCookingLogsAsync(
        Guid userId,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var paging = ZPantryMappings.NormalizePaging(pageIndex, pageSize);

        var query = _dbContext.CookingLogs
            .AsNoTracking()
            .Where(item => item.UserId == userId && !item.IsDeleted)
            .OrderByDescending(item => item.CookedAt)
            .ThenByDescending(item => item.CreatedAt);

        var totalItems = await query.CountAsync(cancellationToken);
        var logs = await query
            .Skip((paging.PageIndex - 1) * paging.PageSize)
            .Take(paging.PageSize)
            .ToListAsync(cancellationToken);

        var logIds = logs.Select(item => item.Id).ToList();
        var usageLogs = await _dbContext.PantryUsageLogs
            .AsNoTracking()
            .Where(item => logIds.Contains(item.CookingLogId) && !item.IsDeleted)
            .ToListAsync(cancellationToken);

        var items = logs.Select(log =>
            ToCookingLogDto(log, usageLogs.Where(item => item.CookingLogId == log.Id).ToList())).ToList();

        return PagedResponse<CookingLogDto>.SuccessPage(items, paging.PageIndex, paging.PageSize, totalItems);
    }

    private static TodayMenuItemDto ToDto(TodayMenuItem item)
        => new()
        {
            Id = item.Id,
            MealId = item.MealId,
            RecipeId = item.RecipeId,
            MealName = item.MealName,
            MealType = item.MealType,
            ServingSize = item.ServingSize,
            PlannedDate = item.PlannedDate,
            Status = item.Status.ToString(),
            Note = item.Note,
            CookedAt = item.CookedAt,
            ImageUrl = item.ImageUrl,
            ImagePublicId = item.ImagePublicId,
            CreatedAt = item.CreatedAt
        };

    private static CookingLogDto ToCookingLogDto(CookingLog cookingLog, IReadOnlyCollection<PantryUsageLog> pantryUsageLogs)
        => new()
        {
            Id = cookingLog.Id,
            TodayMenuItemId = cookingLog.TodayMenuItemId,
            MealId = cookingLog.MealId,
            RecipeId = cookingLog.RecipeId,
            MealName = cookingLog.MealName,
            ImageUrl = cookingLog.ImageUrl,
            ImagePublicId = cookingLog.ImagePublicId,
            CookedAt = cookingLog.CookedAt,
            Rating = cookingLog.Rating,
            Note = cookingLog.Note,
            PantryUsageLogs = pantryUsageLogs.Select(ToPantryUsageLogDto).ToList()
        };

    private static PantryUsageLogDto ToPantryUsageLogDto(PantryUsageLog usageLog)
        => new()
        {
            Id = usageLog.Id,
            IngredientId = usageLog.IngredientId,
            IngredientName = usageLog.IngredientName,
            QuantityUsed = usageLog.QuantityUsed,
            Unit = usageLog.Unit,
            ActionType = usageLog.ActionType,
            Warning = usageLog.Warning
        };

    private static bool IsUnitCompatible(string? pantryUnit, string? recipeUnit)
        => string.IsNullOrWhiteSpace(pantryUnit)
            || string.IsNullOrWhiteSpace(recipeUnit)
            || string.Equals(pantryUnit.Trim(), recipeUnit.Trim(), StringComparison.OrdinalIgnoreCase);

    private static Guid? ResolveRecipeId(Guid? mealId, Guid? recipeId)
    {
        if (recipeId.HasValue && recipeId.Value != Guid.Empty)
        {
            return recipeId.Value;
        }

        if (mealId.HasValue && mealId.Value != Guid.Empty)
        {
            return mealId.Value;
        }

        return null;
    }

    private async Task<List<TodayMenuIngredientDto>> LoadRecipeIngredientsAsync(
        Guid recipeId,
        CancellationToken cancellationToken)
        => await (
                from recipeIngredient in _dbContext.RecipeIngredients.AsNoTracking()
                join ingredient in _dbContext.Ingredients.AsNoTracking()
                    on recipeIngredient.IngredientId equals ingredient.Id
                where recipeIngredient.RecipeId == recipeId
                    && !recipeIngredient.IsDeleted
                    && !ingredient.IsDeleted
                select new TodayMenuIngredientDto
                {
                    IngredientId = ingredient.Id,
                    IngredientName = ingredient.Name,
                    Quantity = recipeIngredient.Quantity,
                    Unit = recipeIngredient.Unit,
                    IsRequired = recipeIngredient.IsRequired
                })
            .ToListAsync(cancellationToken);

    private async Task<List<RecipeIngredientSnapshot>> LoadRecipeIngredientsWithMetadataAsync(
        Guid recipeId,
        CancellationToken cancellationToken)
    {
        var items = await (
                from recipeIngredient in _dbContext.RecipeIngredients.AsNoTracking()
                join ingredient in _dbContext.Ingredients.AsNoTracking()
                    on recipeIngredient.IngredientId equals ingredient.Id
                where recipeIngredient.RecipeId == recipeId
                    && !recipeIngredient.IsDeleted
                    && !ingredient.IsDeleted
                select new RecipeIngredientSnapshot
                {
                    IngredientId = ingredient.Id,
                    IngredientName = ingredient.Name,
                    Quantity = recipeIngredient.Quantity,
                    Unit = recipeIngredient.Unit,
                    IsRequired = recipeIngredient.IsRequired
                })
            .ToListAsync(cancellationToken);

        return items;
    }

    private async Task<List<PantryItemDto>> LoadUserPantryItemsAsync(
        Guid userId,
        CancellationToken cancellationToken)
        => await (
                from pantryItem in _dbContext.UserPantryItems.AsNoTracking()
                join ingredient in _dbContext.Ingredients.AsNoTracking()
                    on pantryItem.IngredientId equals ingredient.Id
                where pantryItem.UserId == userId
                    && !pantryItem.IsDeleted
                    && !ingredient.IsDeleted
                select new PantryItemDto
                {
                    Id = pantryItem.Id,
                    IngredientId = pantryItem.IngredientId,
                    IngredientName = ingredient.Name,
                    Quantity = pantryItem.Quantity,
                    Unit = pantryItem.Unit,
                    ExpiredAt = pantryItem.ExpiredAt,
                    StorageLocation = pantryItem.StorageLocation,
                    Note = pantryItem.Note
                })
            .ToListAsync(cancellationToken);

    private async Task<List<PantryUsageLogDto>> LoadUsageLogsAsync(
        Guid todayMenuItemId,
        CancellationToken cancellationToken)
        => await (
                from usageLog in _dbContext.PantryUsageLogs.AsNoTracking()
                where usageLog.TodayMenuItemId == todayMenuItemId
                    && !usageLog.IsDeleted
                select new PantryUsageLogDto
                {
                    Id = usageLog.Id,
                    IngredientId = usageLog.IngredientId,
                    IngredientName = usageLog.IngredientName,
                    QuantityUsed = usageLog.QuantityUsed,
                    Unit = usageLog.Unit,
                    ActionType = usageLog.ActionType,
                    Warning = usageLog.Warning
                })
            .ToListAsync(cancellationToken);

    private async Task<List<PantryItemDto>> LoadPantryItemsByIdsAsync(
        Guid userId,
        IReadOnlyCollection<Guid> pantryItemIds,
        CancellationToken cancellationToken)
    {
        if (pantryItemIds.Count == 0)
        {
            return [];
        }

        return await (
                from pantryItem in _dbContext.UserPantryItems.AsNoTracking()
                join ingredient in _dbContext.Ingredients.AsNoTracking()
                    on pantryItem.IngredientId equals ingredient.Id
                where pantryItem.UserId == userId
                    && pantryItemIds.Contains(pantryItem.Id)
                    && !ingredient.IsDeleted
                select new PantryItemDto
                {
                    Id = pantryItem.Id,
                    IngredientId = pantryItem.IngredientId,
                    IngredientName = ingredient.Name,
                    Quantity = pantryItem.Quantity,
                    Unit = pantryItem.Unit,
                    ExpiredAt = pantryItem.ExpiredAt,
                    StorageLocation = pantryItem.StorageLocation,
                    Note = pantryItem.Note
                })
            .ToListAsync(cancellationToken);
    }

    private async Task<Dictionary<Guid, string>> LoadPantryIngredientNamesAsync(
        IReadOnlyCollection<UserPantryItem> pantryItems,
        CancellationToken cancellationToken)
    {
        var ingredientIds = pantryItems
            .Select(item => item.IngredientId)
            .Distinct()
            .ToList();

        if (ingredientIds.Count == 0)
        {
            return [];
        }

        return await _dbContext.Ingredients
            .AsNoTracking()
            .Where(item => ingredientIds.Contains(item.Id) && !item.IsDeleted)
            .ToDictionaryAsync(item => item.Id, item => item.Name, cancellationToken);
    }

    private static bool IsIngredientMatch(
        Guid pantryIngredientId,
        IReadOnlyDictionary<Guid, string> pantryIngredientNames,
        RecipeIngredientSnapshot recipeIngredient)
    {
        if (recipeIngredient.IngredientId == pantryIngredientId)
        {
            return true;
        }

        if (!pantryIngredientNames.TryGetValue(pantryIngredientId, out var pantryIngredientName))
        {
            return false;
        }

        return string.Equals(
            NormalizeName(pantryIngredientName),
            NormalizeName(recipeIngredient.IngredientName),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeName(string value)
        => string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();

    private sealed class RecipeIngredientSnapshot
    {
        public Guid IngredientId { get; init; }

        public string IngredientName { get; init; } = string.Empty;

        public decimal? Quantity { get; init; }

        public string? Unit { get; init; }

        public bool IsRequired { get; init; }
    }
}
