using AuthenticationModule.Contracts.Common;
using AuthenticationModule.DTOs;
using AuthenticationModule.Repositories.Entities;
using Microsoft.EntityFrameworkCore;
using ZPantryModule.Services.Interfaces;

namespace ZPantryModule.Services.Implementations;

public class PantryService : IUserPantryService
{
    private readonly ZpantryDbContext _dbContext;

    public PantryService(ZpantryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResponse<PantryItemDto>> GetByUserIdAsync(
        Guid userId,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var paging = ZPantryMappings.NormalizePaging(pageIndex, pageSize);
        var query = _dbContext.UserPantryItems
            .AsNoTracking()
            .Where(item => item.UserId == userId && !item.IsDeleted)
            .OrderBy(item => item.ExpiredAt ?? DateTime.MaxValue)
            .ThenBy(item => item.CreatedAt);

        var totalItems = await query.CountAsync(cancellationToken);
        var pantryItems = await query
            .Skip((paging.PageIndex - 1) * paging.PageSize)
            .Take(paging.PageSize)
            .ToListAsync(cancellationToken);

        return PagedResponse<PantryItemDto>.SuccessPage(
            pantryItems.Select(item => item.ToDto()),
            paging.PageIndex,
            paging.PageSize,
            totalItems);
    }

    public async Task<ApiResponse<PantryItemDto>> UpsertAsync(
        Guid userId,
        UpsertPantryItemRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.IngredientId == Guid.Empty)
        {
            return ApiResponse<PantryItemDto>.Fail("IngredientId is required.");
        }

        var pantryItem = await _dbContext.UserPantryItems.FirstOrDefaultAsync(
            item => item.UserId == userId
                && item.IngredientId == request.IngredientId
                && !item.IsDeleted,
            cancellationToken);

        if (pantryItem is null)
        {
            pantryItem = new UserPantryItem
            {
                UserId = userId,
                IngredientId = request.IngredientId,
                Quantity = request.Quantity,
                Unit = request.Unit,
                ExpiredAt = request.ExpiredAt,
                StorageLocation = request.StorageLocation,
                Note = request.Note
            };
            _dbContext.UserPantryItems.Add(pantryItem);
        }
        else
        {
            if (request.Quantity.HasValue) pantryItem.Quantity = request.Quantity;
            if (request.Unit != null) pantryItem.Unit = request.Unit;
            if (request.ExpiredAt.HasValue) pantryItem.ExpiredAt = request.ExpiredAt;
            if (request.StorageLocation != null) pantryItem.StorageLocation = request.StorageLocation;
            if (request.Note != null) pantryItem.Note = request.Note;
            pantryItem.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponse<PantryItemDto>.SuccessResponse(pantryItem.ToDto(), "Pantry item saved.");
    }

    public async Task<ApiResponse<PantryItemDto>> UpdateAsync(
        Guid userId,
        Guid itemId,
        UpdatePantryItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var pantryItem = await _dbContext.UserPantryItems.FirstOrDefaultAsync(
            item => item.Id == itemId && item.UserId == userId && !item.IsDeleted,
            cancellationToken);

        if (pantryItem is null)
        {
            return ApiResponse<PantryItemDto>.Fail("Pantry item not found.");
        }

        if (request.IngredientId.HasValue && request.IngredientId.Value != Guid.Empty)
        {
            pantryItem.IngredientId = request.IngredientId.Value;
        }

        if (request.Quantity.HasValue)
        {
            pantryItem.Quantity = request.Quantity;
        }

        if (request.Unit != null)
        {
            pantryItem.Unit = request.Unit;
        }

        if (request.ExpiredAt.HasValue)
        {
            pantryItem.ExpiredAt = request.ExpiredAt;
        }

        if (request.StorageLocation != null)
        {
            pantryItem.StorageLocation = request.StorageLocation;
        }

        if (request.Note != null)
        {
            pantryItem.Note = request.Note;
        }

        pantryItem.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponse<PantryItemDto>.SuccessResponse(pantryItem.ToDto(), "Pantry item updated.");
    }

    public async Task<ApiResponse<object>> DeleteAsync(
        Guid userId,
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        var pantryItem = await _dbContext.UserPantryItems.FirstOrDefaultAsync(
            item => item.Id == itemId && item.UserId == userId && !item.IsDeleted,
            cancellationToken);

        if (pantryItem is null)
        {
            return ApiResponse<object>.Fail("Pantry item not found.");
        }

        pantryItem.SoftDelete();
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponse<object>.SuccessResponse(null, "Pantry item deleted.");
    }
}
