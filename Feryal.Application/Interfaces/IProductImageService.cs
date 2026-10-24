
using Feryal.Application.Products.Images;

namespace Feryal.Application.Products.Interfaces;

public interface IProductImageService
{
    Task<IReadOnlyList<ProductImageResponse>> AddImagesAsync(
        int productId,
        IReadOnlyCollection<ProductImageUpload> newImages,
        int? primaryNewImageIndex,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ProductImageResponse>> UpdateImagesAsync(
        int productId,
        IReadOnlyCollection<int> keepImageIds,
        IReadOnlyCollection<ProductImageUpload> newImages,
        int? primaryImageId,
        int? primaryNewImageIndex,
        CancellationToken cancellationToken);

    Task DeleteImageAsync(
        int productId,
        int imageId,
        CancellationToken cancellationToken);
}
