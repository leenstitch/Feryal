//using Feryal.Application.Products.Images;
//using Feryal.Application.Products.Interfaces;
//using Feryal.Domain.Entities;
//using Feryal.Infrastructure.CloudinaryStorage;
//using Feryal.Infrastructure.Persistence;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Logging;

//namespace Feryal.Infrastructure.Products;

//public sealed class ProductImageService : IProductImageService
//{
//    private const int MaxImagesPerProduct = 20;
//    private const long MaxImageSizeBytes = 5 * 1024 * 1024;

//    private static readonly HashSet<string> AllowedExtensions =
//        new(StringComparer.OrdinalIgnoreCase)
//        {
//            ".jpg",
//            ".jpeg",
//            ".png",
//            ".webp"
//        };

//    private static readonly HashSet<string> AllowedContentTypes =
//        new(StringComparer.OrdinalIgnoreCase)
//        {
//            "image/jpeg",
//            "image/png",
//            "image/webp"
//        };

//    private readonly ApplicationDbContext _db;
//    private readonly ICloudinaryImageStorage _storage;
//    private readonly ILogger<ProductImageService> _logger;

//    public ProductImageService(
//        ApplicationDbContext db,
//        ICloudinaryImageStorage storage,
//        ILogger<ProductImageService> logger)
//    {
//        _db = db;
//        _storage = storage;
//        _logger = logger;
//    }

//    public async Task<IReadOnlyList<ProductImageResponse>> AddImagesAsync(
//        int productId,
//        IReadOnlyCollection<ProductImageUpload> newImages,
//        int? primaryNewImageIndex)
//    {
//        ValidateProductId(productId);
//        ValidateFiles(newImages);

//        if (newImages.Count == 0)
//        {
//            throw new ArgumentException(
//                "At least one image is required.",
//                nameof(newImages));
//        }

//        ValidatePrimaryNewIndex(
//            primaryNewImageIndex,
//            newImages.Count);

//        var uploaded = new List<UploadedCloudinaryImage>();
//        var databaseCommitted = false;

//        try
//        {
//            foreach (var image in newImages)
//            {
//                uploaded.Add(await _storage.UploadAsync(
//                    image.Content,
//                    Path.GetFileName(image.FileName),
//                    productId));
//            }

//            await using var transaction =
//                await _db.Database.BeginTransactionAsync();

//            var product = await _db.Set<Product>()
//                .Include(p => p.Images)
//                .FirstOrDefaultAsync(p => p.Id == productId);

//            if (product is null)
//            {
//                throw new KeyNotFoundException(
//                    "Product was not found.");
//            }

//            if (product.Images.Count + uploaded.Count >
//                MaxImagesPerProduct)
//            {
//                throw new InvalidOperationException(
//                    $"A product cannot have more than {MaxImagesPerProduct} images.");
//            }

//            var oldPrimary = product.Images
//                .FirstOrDefault(image => image.IsPrimary);

//            // If an index was supplied, a new image becomes primary.
//            // Otherwise, keep the old primary if one exists.
//            var newPrimaryIndex = primaryNewImageIndex
//                ?? (oldPrimary is null ? 0 : -1);

//            if (newPrimaryIndex >= 0)
//            {
//                foreach (var existingImage in product.Images)
//                {
//                    existingImage.SetPrimary(false);
//                }
//            }

//            var nextOrder = product.Images.Count == 0
//                ? 0
//                : product.Images.Max(image => image.DisplayOrder) + 1;

//            for (var index = 0; index < uploaded.Count; index++)
//            {
//                var cloudImage = uploaded[index];

//                var image = new ProductImage(
//                    product.Id,
//                    cloudImage.ImageUrl,
//                    cloudImage.PublicId,
//                    index == newPrimaryIndex,
//                    nextOrder + index);

//                product.AddImage(image);
//            }

//            // Ensure a product with images has a primary image.
//            var allImages = product.Images.ToList();

//            if (allImages.Count > 0 &&
//                allImages.All(image => !image.IsPrimary))
//            {
//                allImages
//                    .OrderBy(image => image.DisplayOrder)
//                    .First()
//                    .SetPrimary(true);
//            }

//            NormalizeDisplayOrder(allImages);

//            await _db.SaveChangesAsync();
//            await transaction.CommitAsync();

//            databaseCommitted = true;

//            return MapImages(product.Images);
//        }
//        catch
//        {
//            if (!databaseCommitted)
//            {
//                await CleanupUploadedImagesAsync(uploaded);
//            }

//            throw;
//        }
//    }

//    public async Task<IReadOnlyList<ProductImageResponse>> UpdateImagesAsync(
//        int productId,
//        IReadOnlyCollection<int> keepImageIds,
//        IReadOnlyCollection<ProductImageUpload> newImages,
//        int? primaryImageId,
//        int? primaryNewImageIndex)
//    {
//        ValidateProductId(productId);
//        ValidateFiles(newImages);

//        if (keepImageIds.Any(id => id <= 0))
//        {
//            throw new ArgumentException(
//                "Image IDs must be positive.",
//                nameof(keepImageIds));
//        }

//        if (keepImageIds.Distinct().Count() != keepImageIds.Count)
//        {
//            throw new ArgumentException(
//                "Duplicate image IDs are not allowed.",
//                nameof(keepImageIds));
//        }

//        if (primaryImageId.HasValue &&
//            primaryNewImageIndex.HasValue)
//        {
//            throw new ArgumentException(
//                "Choose either an existing primary image or a new primary image.");
//        }

//        ValidatePrimaryNewIndex(
//            primaryNewImageIndex,
//            newImages.Count);

//        if (keepImageIds.Count + newImages.Count >
//            MaxImagesPerProduct)
//        {
//            throw new InvalidOperationException(
//                $"A product cannot have more than {MaxImagesPerProduct} images.");
//        }

//        var uploaded = new List<UploadedCloudinaryImage>();
//        var databaseCommitted = false;

//        try
//        {
//            // Upload the new images first.
//            foreach (var image in newImages)
//            {
//                uploaded.Add(await _storage.UploadAsync(
//                    image.Content,
//                    Path.GetFileName(image.FileName),
//                    productId));
//            }

//            await using var transaction =
//                await _db.Database.BeginTransactionAsync();

//            var product = await _db.Set<Product>()
//                .Include(p => p.Images)
//                .FirstOrDefaultAsync(p => p.Id == productId);

//            if (product is null)
//            {
//                throw new KeyNotFoundException(
//                    "Product was not found.");
//            }

//            var currentImages = product.Images.ToList();

//            // Ensure all requested image IDs belong to this product.
//            if (keepImageIds.Any(
//                id => currentImages.All(image => image.Id != id)))
//            {
//                throw new ArgumentException(
//                    "One or more image IDs do not belong to this product.");
//            }

//            if (keepImageIds.Count + uploaded.Count >
//                MaxImagesPerProduct)
//            {
//                throw new InvalidOperationException(
//                    $"A product cannot have more than {MaxImagesPerProduct} images.");
//            }

//            var retainedImages = currentImages
//                .Where(image => keepImageIds.Contains(image.Id))
//                .OrderBy(image => image.DisplayOrder)
//                .ToList();

//            var removedImages = currentImages
//                .Where(image => !keepImageIds.Contains(image.Id))
//                .ToList();

//            if (primaryImageId.HasValue &&
//                retainedImages.All(
//                    image => image.Id != primaryImageId.Value))
//            {
//                throw new ArgumentException(
//                    "The primary image must be one of the retained images.");
//            }

//            var oldPrimaryId = currentImages
//                .FirstOrDefault(image => image.IsPrimary)
//                ?.Id;

//            // Choose the primary image:
//            // 1. Use the requested existing image, if supplied.
//            // 2. Otherwise use the requested new image, if supplied.
//            // 3. Otherwise retain the old primary if it is kept.
//            // 4. Otherwise choose the first retained image.
//            // 5. If no images are retained, choose the first new image.
//            int? chosenPrimaryId = primaryImageId;

//            if (!primaryImageId.HasValue &&
//                !primaryNewImageIndex.HasValue)
//            {
//                if (oldPrimaryId.HasValue &&
//                    retainedImages.Any(
//                        image => image.Id == oldPrimaryId.Value))
//                {
//                    chosenPrimaryId = oldPrimaryId.Value;
//                }
//                else
//                {
//                    chosenPrimaryId = retainedImages
//                        .FirstOrDefault()
//                        ?.Id;
//                }
//            }

//            // Remove old images from the aggregate and EF tracking.
//            foreach (var image in removedImages)
//            {
//                product.RemoveImage(image);
//            }

//            _db.Set<ProductImage>().RemoveRange(removedImages);

//            // Clear primary status before choosing the final primary image.
//            foreach (var image in retainedImages)
//            {
//                image.SetPrimary(false);
//            }

//            // Normalize retained image order first.
//            for (var index = 0; index < retainedImages.Count; index++)
//            {
//                retainedImages[index].SetDisplayOrder(index);
//            }

//            var newEntities = new List<ProductImage>();

//            for (var index = 0; index < uploaded.Count; index++)
//            {
//                var cloudImage = uploaded[index];

//                var image = new ProductImage(
//                    product.Id,
//                    cloudImage.ImageUrl,
//                    cloudImage.PublicId,
//                    isPrimary: false,
//                    displayOrder: retainedImages.Count + index);

//                newEntities.Add(image);
//                product.AddImage(image);
//            }

//            var finalImages = retainedImages
//                .Concat(newEntities)
//                .ToList();

//            if (primaryImageId.HasValue)
//            {
//                var selectedImage = retainedImages.First(
//                    image => image.Id == primaryImageId.Value);

//                selectedImage.SetPrimary(true);
//            }
//            else if (primaryNewImageIndex.HasValue)
//            {
//                newEntities[primaryNewImageIndex.Value]
//                    .SetPrimary(true);
//            }
//            else if (chosenPrimaryId.HasValue)
//            {
//                var selectedImage = retainedImages.First(
//                    image => image.Id == chosenPrimaryId.Value);

//                selectedImage.SetPrimary(true);
//            }
//            else if (finalImages.Count > 0)
//            {
//                finalImages[0].SetPrimary(true);
//            }

//            // Final safety check: exactly one primary image when images exist.
//            if (finalImages.Count > 0 &&
//                finalImages.Count(image => image.IsPrimary) != 1)
//            {
//                foreach (var image in finalImages)
//                {
//                    image.SetPrimary(false);
//                }

//                finalImages[0].SetPrimary(true);
//            }

//            NormalizeDisplayOrder(finalImages);

//            await _db.SaveChangesAsync();
//            await transaction.CommitAsync();

//            databaseCommitted = true;

//            // The database is committed. Delete removed Cloudinary assets.
//            foreach (var image in removedImages)
//            {
//                try
//                {
//                    await _storage.DeleteAsync(image.PublicId);
//                }
//                catch (Exception exception)
//                {
//                    _logger.LogError(
//                        exception,
//                        "Could not delete Cloudinary image {PublicId} for product {ProductId}.",
//                        image.PublicId,
//                        productId);
//                }
//            }

//            return MapImages(finalImages);
//        }
//        catch
//        {
//            if (!databaseCommitted)
//            {
//                await CleanupUploadedImagesAsync(uploaded);
//            }

//            throw;
//        }
//    }

//    public async Task DeleteImageAsync(int productId, int imageId)
//    {
//        ValidateProductId(productId);

//        if (imageId <= 0)
//        {
//            throw new ArgumentException(
//                "Image ID must be positive.",
//                nameof(imageId));
//        }

//        string publicId;

//        await using (var transaction =
//            await _db.Database.BeginTransactionAsync())
//        {
//            var product = await _db.Set<Product>()
//                .Include(p => p.Images)
//                .FirstOrDefaultAsync(p => p.Id == productId);

//            if (product is null)
//            {
//                throw new KeyNotFoundException(
//                    "Product was not found.");
//            }

//            var image = product.Images
//                .FirstOrDefault(item => item.Id == imageId);

//            if (image is null)
//            {
//                throw new KeyNotFoundException(
//                    "Image was not found for this product.");
//            }

//            publicId = image.PublicId;
//            var wasPrimary = image.IsPrimary;

//            product.RemoveImage(image);
//            _db.Set<ProductImage>().Remove(image);

//            var remainingImages = product.Images.ToList();

//            if (wasPrimary &&
//                remainingImages.Count > 0)
//            {
//                foreach (var remainingImage in remainingImages)
//                {
//                    remainingImage.SetPrimary(false);
//                }

//                remainingImages
//                    .OrderBy(item => item.DisplayOrder)
//                    .First()
//                    .SetPrimary(true);
//            }

//            NormalizeDisplayOrder(remainingImages);

//            await _db.SaveChangesAsync();
//            await transaction.CommitAsync();
//        }

//        // Delete from Cloudinary after the database transaction succeeds.
//        try
//        {
//            await _storage.DeleteAsync(publicId);
//        }
//        catch (Exception exception)
//        {
//            _logger.LogError(
//                exception,
//                "Could not delete Cloudinary image {PublicId} for product {ProductId}.",
//                publicId,
//                productId);

//            throw new InvalidOperationException(
//                "The image was removed from the product, but Cloudinary deletion failed. Check the application logs.");
//        }
//    }

//    private static void ValidateProductId(int productId)
//    {
//        if (productId <= 0)
//        {
//            throw new ArgumentException(
//                "Product ID must be positive.",
//                nameof(productId));
//        }
//    }

//    private static void ValidatePrimaryNewIndex(
//        int? primaryNewImageIndex,
//        int newImageCount)
//    {
//        if (primaryNewImageIndex.HasValue &&
//            (primaryNewImageIndex.Value < 0 ||
//             primaryNewImageIndex.Value >= newImageCount))
//        {
//            throw new ArgumentException(
//                "The selected new primary image index is invalid.",
//                nameof(primaryNewImageIndex));
//        }
//    }

//    private static void ValidateFiles(
//        IReadOnlyCollection<ProductImageUpload> images)
//    {
//        ArgumentNullException.ThrowIfNull(images);

//        if (images.Count > MaxImagesPerProduct)
//        {
//            throw new ArgumentException(
//                $"A maximum of {MaxImagesPerProduct} images can be uploaded at once.",
//                nameof(images));
//        }

//        foreach (var image in images)
//        {
//            if (image is null)
//            {
//                throw new ArgumentException(
//                    "The image collection cannot contain null items.",
//                    nameof(images));
//            }

//            var extension = Path.GetExtension(image.FileName);

//            if (image.Length <= 0 ||
//                image.Length > MaxImageSizeBytes)
//            {
//                throw new ArgumentException(
//                    "Each image must be larger than zero and no larger than 5 MB.");
//            }

//            if (!AllowedExtensions.Contains(extension) ||
//                !AllowedContentTypes.Contains(image.ContentType))
//            {
//                throw new ArgumentException(
//                    "Only JPG, PNG, and WebP images are allowed.");
//            }

//            if (image.Content is null ||
//                !image.Content.CanRead)
//            {
//                throw new ArgumentException(
//                    "One of the image streams cannot be read.");
//            }
//        }
//    }

//    private static void NormalizeDisplayOrder(
//        IEnumerable<ProductImage> images)
//    {
//        var orderedImages = images
//            .OrderBy(image => image.DisplayOrder)
//            .ThenBy(image => image.Id)
//            .ToList();

//        for (var index = 0; index < orderedImages.Count; index++)
//        {
//            orderedImages[index].SetDisplayOrder(index);
//        }
//    }

//    private static IReadOnlyList<ProductImageResponse> MapImages(
//        IEnumerable<ProductImage> images)
//    {
//        return images
//            .OrderBy(image => image.DisplayOrder)
//            .Select(image => new ProductImageResponse
//            {
//                Id = image.Id,
//                ProductId = image.ProductId,
//                ImageUrl = image.ImageUrl,
//                IsPrimary = image.IsPrimary,
//                DisplayOrder = image.DisplayOrder
//            })
//            .ToList();
//    }

//    private async Task CleanupUploadedImagesAsync(
//        IEnumerable<UploadedCloudinaryImage> uploaded)
//    {
//        foreach (var image in uploaded)
//        {
//            try
//            {
//                await _storage.DeleteAsync(image.PublicId);
//            }
//            catch (Exception exception)
//            {
//                _logger.LogError(
//                    exception,
//                    "Failed to clean up uploaded Cloudinary image {PublicId}.",
//                    image.PublicId);
//            }
//        }
//    }
//}