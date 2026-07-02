using AuthenticationModule.Contracts.Common;
using Microsoft.AspNetCore.Mvc;
using ZPantryModule.DTOs;
using ZPantryModule.Services.Interfaces;

namespace ZPantryModule.Controllers;

[ApiController]
[Route("api/media")]
public class MediaController : ControllerBase
{
    private readonly ICloudinaryStorageService _cloudinaryStorageService;

    public MediaController(ICloudinaryStorageService cloudinaryStorageService)
    {
        _cloudinaryStorageService = cloudinaryStorageService;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<ApiResponse<string>> Upload([FromForm] UploadMediaRequest request)
    {
        await using var stream = request.File.OpenReadStream();
        return await _cloudinaryStorageService.UploadAsync(
            stream,
            request.File.FileName,
            request.IngredientId,
            request.RecipeId);
    }

    [HttpDelete]
    public Task<ApiResponse<object>> Delete([FromQuery] string publicId)
        => _cloudinaryStorageService.DeleteAsync(publicId);
}
