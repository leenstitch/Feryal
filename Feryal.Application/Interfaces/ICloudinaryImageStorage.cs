namespace Feryal.Application.Interfaces;

public sealed record CloudinaryImageResult(
    string ImageUrl,
    string PublicId);

public interface ICloudinaryImageStorage
{
    Task<CloudinaryImageResult> UploadAsync(
        Stream stream,
        string fileName,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        string publicId,
        CancellationToken cancellationToken);
}