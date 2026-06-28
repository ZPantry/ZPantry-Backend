using AuthenticationModule.Contracts.Common;
using ZPantryModule.Services.Interfaces;

namespace ZPantryModule.Services.Implementations;

public class CloudinaryStorageService : ICloudinaryStorageService
{
    public Task<ApiResponse<string>> UploadAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default)
        => Task.FromResult(ApiResponse<string>.Fail("Cloudinary service not implemented yet."));

    public Task<ApiResponse<object>> DeleteAsync(string publicId, CancellationToken cancellationToken = default)
        => Task.FromResult(ApiResponse<object>.Fail("Cloudinary service not implemented yet."));
}

