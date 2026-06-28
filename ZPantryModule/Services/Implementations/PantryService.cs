using AuthenticationModule.Contracts.Common;
using AuthenticationModule.DTOs;
using ZPantryModule.Services.Interfaces;

namespace ZPantryModule.Services.Implementations;

public class PantryService : IUserPantryService
{
    public Task<PagedResponse<PantryItemDto>> GetByUserIdAsync(Guid userId, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
        => Task.FromResult(PagedResponse<PantryItemDto>.FailPage("Pantry service not implemented yet."));

    public Task<ApiResponse<PantryItemDto>> UpsertAsync(Guid userId, UpsertPantryItemRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(ApiResponse<PantryItemDto>.Fail("Pantry service not implemented yet."));

    public Task<ApiResponse<PantryItemDto>> UpdateAsync(Guid userId, Guid itemId, UpsertPantryItemRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(ApiResponse<PantryItemDto>.Fail("Pantry service not implemented yet."));

    public Task<ApiResponse<object>> DeleteAsync(Guid userId, Guid itemId, CancellationToken cancellationToken = default)
        => Task.FromResult(ApiResponse<object>.Fail("Pantry service not implemented yet."));
}

