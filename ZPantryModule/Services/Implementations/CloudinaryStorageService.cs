using AuthenticationModule.Contracts.Common;
using Microsoft.Extensions.Configuration;
using ZPantryModule.Services.Interfaces;

namespace ZPantryModule.Services.Implementations;

public class CloudinaryStorageService : ICloudinaryStorageService
{
    private readonly IConfiguration _configuration;

    public CloudinaryStorageService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<ApiResponse<string>> UploadAsync(
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured())
        {
            return ApiResponse<string>.Fail("Cloudinary configuration is missing.");
        }

        if (fileStream.Length == 0)
        {
            return ApiResponse<string>.Fail("Uploaded file is empty.");
        }

        await Task.CompletedTask;
        return ApiResponse<string>.Fail("Cloudinary upload provider is not wired yet.");
    }

    public Task<ApiResponse<object>> DeleteAsync(string publicId, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured())
        {
            return Task.FromResult(ApiResponse<object>.Fail("Cloudinary configuration is missing."));
        }

        if (string.IsNullOrWhiteSpace(publicId))
        {
            return Task.FromResult(ApiResponse<object>.Fail("PublicId is required."));
        }

        return Task.FromResult(ApiResponse<object>.Fail("Cloudinary delete provider is not wired yet."));
    }

    private bool IsConfigured()
        => !string.IsNullOrWhiteSpace(_configuration["CLOUDINARY_CLOUD_NAME"])
            && !string.IsNullOrWhiteSpace(_configuration["CLOUDINARY_API_KEY"])
            && !string.IsNullOrWhiteSpace(_configuration["CLOUDINARY_API_SECRET"]);
}
