using Microsoft.AspNetCore.Http;

namespace ZPantryModule.DTOs;

public class UploadMediaRequest
{
    public IFormFile File { get; set; } = default!;
}

