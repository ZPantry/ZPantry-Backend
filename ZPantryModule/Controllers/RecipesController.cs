using AuthenticationModule.Contracts.Common;
using AuthenticationModule.DTOs;
using Microsoft.AspNetCore.Mvc;
using ZPantryModule.DTOs;
using ZPantryModule.Services.Interfaces;

namespace ZPantryModule.Controllers;

[ApiController]
[Route("api/recipes")]
public class RecipesController : ControllerBase
{
    private readonly IRecipeService _recipeService;

    public RecipesController(IRecipeService recipeService)
    {
        _recipeService = recipeService;
    }

    [HttpGet]
    public Task<PagedResponse<RecipeDto>> GetAll([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        => _recipeService.GetAllAsync(pageIndex, pageSize);

    [HttpPost]
    public Task<ApiResponse<RecipeDto>> Create([FromBody] CreateRecipeRequest request)
        => _recipeService.CreateAsync(request);

    [HttpPost("/api/v2/recipes")]
    [Consumes("multipart/form-data")]
    public Task<ApiResponse<RecipeDto>> CreateV2([FromForm] CreateRecipeFormRequest request)
        => _recipeService.CreateV2Async(request);

    [HttpGet("{id:guid}")]
    public Task<ApiResponse<RecipeDto>> GetById(Guid id)
        => _recipeService.GetByIdAsync(id);

    [HttpPut("{id:guid}")]
    public Task<ApiResponse<RecipeDto>> Update(Guid id, [FromBody] UpdateRecipeRequest request)
        => _recipeService.UpdateAsync(id, request);

    [HttpPut("/api/v2/recipes/{id:guid}")]
    [Consumes("multipart/form-data")]
    public Task<ApiResponse<RecipeDto>> UpdateV2(Guid id, [FromForm] UpdateRecipeFormRequest request)
        => _recipeService.UpdateV2Async(id, request);

    [HttpDelete("{id:guid}")]
    public Task<ApiResponse<object>> Delete(Guid id)
        => _recipeService.DeleteAsync(id);
}
