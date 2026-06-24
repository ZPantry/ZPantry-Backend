using CuisineModule.Repositories.Entities;
using Microsoft.AspNetCore.Mvc;
using HttpGetAttribute = Microsoft.AspNetCore.Mvc.HttpGetAttribute;
using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;

[ApiController]
[Route("api/[controller]")]
[Microsoft.AspNetCore.Mvc.ApiExplorerSettings(GroupName = "cuisine")]
public class IngredientController : ControllerBase
{
    private readonly IIngredientService _ingredientService;
    public IngredientController(IIngredientService ingredientService)
    {
        _ingredientService = ingredientService;
    }

    [HttpGet("get-all")]
    public async Task<ActionResult<List<Ingredient>>> GetAllIngredientsAsync()
    {
        try
        {
            var ingredients = await _ingredientService.GetAllIngredientsAsync();
            return Ok(ingredients);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = ex.Message });
        }
    }

    [HttpGet("get-all/{userId}")]
    public async Task<ActionResult<List<Ingredient>>> GetIngredientsByUserId([FromRoute] Guid userId)
    {
        try
        {
            var ingredients = await _ingredientService.GetIngredientsByUserId(userId);
            return Ok(ingredients);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = ex.Message });
        }
    }

    [HttpPost("add-ingredient")]
    public async Task<IActionResult> AddIngredient([FromBody] IngredientRequest request)
    {
        try
        {
            await _ingredientService.AddIngredient(request);
            return Ok(new { Message = "Add ingredient successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = ex.Message });
        }
    }

    [HttpPut("update-ingredient/{ingredientId}")]
    public async Task<IActionResult> UpdateIngredient([FromRoute] Guid ingredientId, [FromBody] IngredientRequest request)
    {
        try
        {
            await _ingredientService.UpdateIngredient(ingredientId, request);
            return Ok(new { Message = "Update ingredient successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = ex.Message });
        }
    }

    [HttpDelete("delete-ingredient/{ingredientId}")]
    public async Task<IActionResult> DeleteIngredient([FromRoute] Guid ingredientId)
    {
        try
        {
            await _ingredientService.DeleteIngredient(ingredientId);
            return Ok(new { Message = "Delete ingredient successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = ex.Message });
        }
    }
}