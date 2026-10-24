namespace Feryal.Application.Interfaces;

// Represents the result of uploading an image to Cloudinary.
public sealed record CloudinaryImageResult(
    string ImageUrl,
    string PublicId);

// Defines the contract for uploading and deleting images stored in Cloudinary.
public interface ICloudinaryImageStorage
{
    // Uploads an image stream to Cloudinary and returns its URL and public ID.
    Task<CloudinaryImageResult> UploadAsync(
        Stream stream,
        string fileName,
        CancellationToken cancellationToken);

    // Deletes an image from Cloudinary using its public ID.
    Task DeleteAsync(
        string publicId,
        CancellationToken cancellationToken);
}