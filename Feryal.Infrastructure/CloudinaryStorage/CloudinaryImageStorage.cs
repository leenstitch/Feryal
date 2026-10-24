using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Feryal.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Feryal.Infrastructure.CloudinaryStorage;

public sealed class CloudinaryImageStorage : ICloudinaryImageStorage
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryImageStorage(IConfiguration configuration)
    {
        var cloudName = configuration["Cloudinary:CloudName"];
        var apiKey = configuration["Cloudinary:ApiKey"];
        var apiSecret = configuration["Cloudinary:ApiSecret"];

        if (string.IsNullOrWhiteSpace(cloudName) ||
            string.IsNullOrWhiteSpace(apiKey) ||
            string.IsNullOrWhiteSpace(apiSecret))
        {
            throw new InvalidOperationException(
                "Cloudinary configuration is missing.");
        }

        var account = new Account(cloudName, apiKey, apiSecret);
        _cloudinary = new Cloudinary(account);
    }

    public async Task<CloudinaryImageResult> UploadAsync(
        Stream stream,
        string fileName,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var parameters = new ImageUploadParams
        {
            File = new FileDescription(fileName, stream),
            Folder = "feryal/products"
        };

        var result = await _cloudinary.UploadAsync(parameters);

        cancellationToken.ThrowIfCancellationRequested();

        if (result.Error is not null ||
            string.IsNullOrWhiteSpace(result.SecureUrl?.ToString()) ||
            string.IsNullOrWhiteSpace(result.PublicId))
        {
            throw new InvalidOperationException(
                result.Error?.Message ?? "Cloudinary image upload failed.");
        }

        return new CloudinaryImageResult(
            result.SecureUrl.ToString(),
            result.PublicId);
    }

    public async Task DeleteAsync(
        string publicId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(publicId))
            throw new ArgumentException(
                "Public ID is required.",
                nameof(publicId));

        cancellationToken.ThrowIfCancellationRequested();

        var result = await _cloudinary.DestroyAsync(
            new DeletionParams(publicId));

        cancellationToken.ThrowIfCancellationRequested();

        if (result.Error is not null)
        {
            throw new InvalidOperationException(
                result.Error.Message);
        }

        if (result.Result is not "ok" and not "not found")
        {
            throw new InvalidOperationException(
                $"Cloudinary deletion returned: {result.Result}");
        }
    }
}