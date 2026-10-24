using Feryal.Application.Products.Images;

// Defines the contract for managing product images, including uploading, updating, and deleting images.
public interface IProductImageService
{
    // Adds new images to a product and optionally sets one of the newly added images as the primary image.
    Task<IReadOnlyList<ProductImageResponse>> AddImagesAsync(
        int productId,
        IReadOnlyCollection<ProductImageUpload> newImages,
        int? primaryNewImageIndex,
        CancellationToken cancellationToken);

    // Updates a product's images by retaining selected existing images, adding new images, and optionally setting a primary image.
    Task<IReadOnlyList<ProductImageResponse>> UpdateImagesAsync(
        int productId,
        IReadOnlyCollection<int> keepImageIds,
        IReadOnlyCollection<ProductImageUpload> newImages,
        int? primaryImageId,
        int? primaryNewImageIndex,
        CancellationToken cancellationToken);

    // Deletes a specific image belonging to the specified product.
    Task DeleteImageAsync(
        int productId,
        int imageId,
        CancellationToken cancellationToken);
}