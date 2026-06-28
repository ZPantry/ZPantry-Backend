using AuthenticationModule.Contracts.Common;
using AuthenticationModule.DTOs;
using ZPantryModule.Services.Interfaces;

namespace ZPantryModule.Services.Implementations;

public class RecipeService : IRecipeService
{
    public Task<PagedResponse<RecipeDto>> GetAllAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
        => Task.FromResult(PagedResponse<RecipeDto>.FailPage("Recipe service not implemented yet."));

    public Task<ApiResponse<RecipeDto>> CreateAsync(CreateRecipeRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(ApiResponse<RecipeDto>.Fail("Recipe service not implemented yet."));

    public Task<ApiResponse<RecipeDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(ApiResponse<RecipeDto>.Fail("Recipe service not implemented yet."));

    public Task<ApiResponse<RecipeDto>> UpdateAsync(Guid id, UpdateRecipeRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(ApiResponse<RecipeDto>.Fail("Recipe service not implemented yet."));

    public Task<ApiResponse<object>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(ApiResponse<object>.Fail("Recipe service not implemented yet."));
}

