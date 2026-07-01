using AuthenticationModule.Contracts.Common;
using AuthenticationModule.DTOs;
using Microsoft.AspNetCore.Mvc;
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

    [HttpPut("{id:guid}")]
    public Task<ApiResponse<IngredientDto>> Update(Guid id, [FromBody] UpdateIngredientRequest request)
        => _ingredientService.UpdateAsync(id, request);

    [HttpDelete("{id:guid}")]
    public Task<ApiResponse<object>> Delete(Guid id)
        => _ingredientService.DeleteAsync(id);
}
