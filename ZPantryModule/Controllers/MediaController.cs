using AuthenticationModule.Contracts.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
    public Task<ApiResponse<string>> Upload([FromForm] UploadMediaRequest request)
        => _cloudinaryStorageService.UploadAsync(Stream.Null, request.File.FileName);

    [HttpDelete("{id:guid}")]
    public Task<ApiResponse<object>> Delete(Guid id)
        => _cloudinaryStorageService.DeleteAsync(id.ToString());
}

public class UploadMediaRequest
{
    public IFormFile File { get; set; } = default!;
}
