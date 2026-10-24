using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Feryal.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Feryal.Infrastructure.CloudinaryStorage;

// Implements image storage operations using Cloudinary.
public sealed class CloudinaryImageStorage : ICloudinaryImageStorage
{
    // Client used to upload and delete images in Cloudinary.
    private readonly Cloudinary _cloudinary;

    // Initializes Cloudinary using credentials from the application configuration.
    public CloudinaryImageStorage(IConfiguration configuration)
    {
        // Read the Cloudinary account credentials from configuration.
        var cloudName = configuration["Cloudinary:CloudName"];
        var apiKey = configuration["Cloudinary:ApiKey"];
        var apiSecret = configuration["Cloudinary:ApiSecret"];

        // Ensure all required Cloudinary credentials are configured.
        if (string.IsNullOrWhiteSpace(cloudName) ||
            string.IsNullOrWhiteSpace(apiKey) ||
            string.IsNullOrWhiteSpace(apiSecret))
        {
            throw new InvalidOperationException(
                "Cloudinary configuration is missing.");
        }

        // Create the Cloudinary account and initialize the client.
        var account = new Account(cloudName, apiKey, apiSecret);
        _cloudinary = new Cloudinary(account);
    }

    // Uploads an image to Cloudinary and returns its secure URL and public ID.
    public async Task<CloudinaryImageResult> UploadAsync(
        Stream stream,
        string fileName,
        CancellationToken cancellationToken)
    {
        // Stop the operation if cancellation has already been requested.
        cancellationToken.ThrowIfCancellationRequested();

        // Configure the upload using the provided stream and destination folder.
        var parameters = new ImageUploadParams
        {
            File = new FileDescription(fileName, stream),
            Folder = "feryal/products"
        };

        // Upload the image to Cloudinary.
        var result = await _cloudinary.UploadAsync(parameters);

        // Check whether cancellation was requested during the upload.
        cancellationToken.ThrowIfCancellationRequested();

        // Ensure the upload succeeded and returned the required image identifiers.
        if (result.Error is not null ||
            string.IsNullOrWhiteSpace(result.SecureUrl?.ToString()) ||
            string.IsNullOrWhiteSpace(result.PublicId))
        {
            throw new InvalidOperationException(
                result.Error?.Message ?? "Cloudinary image upload failed.");
        }

        // Return the secure image URL and Cloudinary public ID.
        return new CloudinaryImageResult(
            result.SecureUrl.ToString(),
            result.PublicId);
    }

    // Deletes an image from Cloudinary using its public ID.
    public async Task DeleteAsync(
        string publicId,
        CancellationToken cancellationToken)
    {
        // Validate that a public ID was provided.
        if (string.IsNullOrWhiteSpace(publicId))
            throw new ArgumentException(
                "Public ID is required.",
                nameof(publicId));

        // Stop the operation if cancellation has already been requested.
        cancellationToken.ThrowIfCancellationRequested();

        // Send a deletion request for the specified Cloudinary image.
        var result = await _cloudinary.DestroyAsync(
            new DeletionParams(publicId));

        // Check whether cancellation was requested during the deletion.
        cancellationToken.ThrowIfCancellationRequested();

        // Throw an exception if Cloudinary reports an error.
        if (result.Error is not null)
        {
            throw new InvalidOperationException(
                result.Error.Message);
        }

        // Accept successful deletion or an image that no longer exists.
        if (result.Result is not "ok" and not "not found")
        {
            throw new InvalidOperationException(
                $"Cloudinary deletion returned: {result.Result}");
        }
    }
}