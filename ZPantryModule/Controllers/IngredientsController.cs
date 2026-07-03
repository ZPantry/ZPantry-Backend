using AuthenticationModule.Contracts.Common;
using AuthenticationModule.DTOs;
using Microsoft.AspNetCore.Mvc;
using ZPantryModule.DTOs;
using ZPantryModule.Services.Interfaces;

namespace ZPantryModule.Controllers;

[ApiController]
[Route("api/ingredients")]
public class IngredientsController : ControllerBase
{
    private readonly IIngredientService _ingredientService;

    public IngredientsController(IIngredientService ingredientService)
    {
        _ingredientService = ingredientService;
    }

    [HttpGet]
    public Task<PagedResponse<IngredientDto>> GetAll(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
        => _ingredientService.GetAllAsync(pageIndex, pageSize, search);

    [HttpPost]
    public Task<ApiResponse<IngredientDto>> Create([FromBody] CreateIngredientRequest request)
        => _ingredientService.CreateAsync(request);

    [HttpPost("/api/v2/ingredients")]
    [Consumes("multipart/form-data")]
    public Task<ApiResponse<IngredientDto>> CreateV2([FromForm] CreateIngredientFormRequest request)
        => _ingredientService.CreateV2Async(request);

    [HttpPut("{id:guid}")]
    public Task<ApiResponse<IngredientDto>> Update(Guid id, [FromBody] UpdateIngredientRequest request)
        => _ingredientService.UpdateAsync(id, request);

    [HttpPut("/api/v2/ingredients/{id:guid}")]
    [Consumes("multipart/form-data")]
    public Task<ApiResponse<IngredientDto>> UpdateV2(Guid id, [FromForm] UpdateIngredientFormRequest request)
        => _ingredientService.UpdateV2Async(id, request);

    [HttpDelete("{id:guid}")]
    public Task<ApiResponse<object>> Delete(Guid id)
        => _ingredientService.DeleteAsync(id);
}
