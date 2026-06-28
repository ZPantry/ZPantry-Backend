using AuthenticationModule.Contracts.Common;
using AuthenticationModule.DTOs;
using ZPantryModule.Services.Interfaces;

namespace ZPantryModule.Services.Implementations;

public class IngredientService : IIngredientService
{
    public Task<PagedResponse<IngredientDto>> GetAllAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
        => Task.FromResult(PagedResponse<IngredientDto>.FailPage("Ingredient service not implemented yet."));

    public Task<ApiResponse<IngredientDto>> CreateAsync(CreateIngredientRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(ApiResponse<IngredientDto>.Fail("Ingredient service not implemented yet."));

    public Task<ApiResponse<IngredientDto>> UpdateAsync(Guid id, UpdateIngredientRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(ApiResponse<IngredientDto>.Fail("Ingredient service not implemented yet."));

    public Task<ApiResponse<object>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(ApiResponse<object>.Fail("Ingredient service not implemented yet."));
}

