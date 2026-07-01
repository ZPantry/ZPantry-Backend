using AuthenticationModule.Contracts.Common;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
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

        try
        {
            var cloudinary = GetCloudinaryClient();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, fileStream)
            };

            var preset = GetPresetName();
            if (!string.IsNullOrWhiteSpace(preset))
            {
                uploadParams.UploadPreset = preset;
            }
            else
            {
                uploadParams.Folder = "zpantry";
            }

            var uploadResult = await cloudinary.UploadAsync(uploadParams, cancellationToken);

            if (uploadResult.Error != null)
            {
                return ApiResponse<string>.Fail($"Cloudinary upload failed: {uploadResult.Error.Message}");
            }

            return ApiResponse<string>.SuccessResponse(uploadResult.SecureUrl.ToString(), "File uploaded successfully.");
        }
        catch (Exception ex)
        {
            return ApiResponse<string>.Fail($"Exception during upload: {ex.Message}");
        }
    }

    public async Task<ApiResponse<object>> DeleteAsync(string publicId, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured())
        {
            return ApiResponse<object>.Fail("Cloudinary configuration is missing.");
        }

        if (string.IsNullOrWhiteSpace(publicId))
        {
            return ApiResponse<object>.Fail("PublicId is required.");
        }

        try
        {
            var cloudinary = GetCloudinaryClient();
            var deletionParams = new DeletionParams(publicId);
            var result = await cloudinary.DestroyAsync(deletionParams);

            if (result.Result == "ok" || result.Result == "not found")
            {
                return ApiResponse<object>.SuccessResponse(null, "File deleted successfully.");
            }

            return ApiResponse<object>.Fail($"Cloudinary delete failed: {result.Result}");
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.Fail($"Exception during delete: {ex.Message}");
        }
    }

    private Cloudinary GetCloudinaryClient()
    {
        var account = new Account(GetCloudName(), GetApiKey(), GetApiSecret());
        return new Cloudinary(account);
    }

    private string GetCloudName()
        => _configuration["Cloudinary_Name"] ?? _configuration["CLOUDINARY_CLOUD_NAME"] ?? _configuration["Cloudinary:Name"] ?? string.Empty;

    private string GetApiKey()
        => _configuration["Cloudinary_API_Key"] ?? _configuration["CLOUDINARY_API_KEY"] ?? _configuration["Cloudinary:ApiKey"] ?? string.Empty;

    private string GetApiSecret()
        => _configuration["Cloudinary_API_Secret"] ?? _configuration["CLOUDINARY_API_SECRET"] ?? _configuration["Cloudinary:ApiSecret"] ?? string.Empty;

    private string GetPresetName()
        => _configuration["Cloudinary_PresetName"] ?? _configuration["CLOUDINARY_PRESET_NAME"] ?? _configuration["Cloudinary:PresetName"] ?? string.Empty;

    private bool IsConfigured()
        => !string.IsNullOrWhiteSpace(GetCloudName())
            && !string.IsNullOrWhiteSpace(GetApiKey())
            && !string.IsNullOrWhiteSpace(GetApiSecret());
}
