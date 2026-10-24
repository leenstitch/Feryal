using System.Data;
using System.Text.Json;
using Feryal.Application.Events.Products;
using Feryal.Application.Interfaces;
using Feryal.Application.Products.Images;
using Feryal.Application.Products.Interfaces;
using Feryal.Domain.Entities;
using Feryal.Application.Interfaces;
using Microsoft.Extensions.Logging;

// Provides product image management operations using Cloudinary storage and database transactions.
namespace Feryal.Infrastructure.Products;

public sealed class ProductImageService : IProductImageService
{
    // Defines the maximum number of images allowed for a product.
    private const int MaximumImagesPerProduct = 20;

    // Defines the maximum allowed image file size in bytes (5 MB).
    private const long MaximumFileSize = 5 * 1024 * 1024;

    // Lists the file extensions accepted for uploaded images.
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

    // Lists the MIME content types accepted for uploaded images.
    private static readonly HashSet<string> AllowedContentTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

    // Provides database operations for products.
    private readonly IProductRepository _productRepository;

    // Provides database operations for product images.
    private readonly IRepository<ProductImage> _imageRepository;

    // Handles uploading and deleting images in Cloudinary.
    private readonly ICloudinaryImageStorage _storage;

    // Stores cache invalidation events in the outbox.
    private readonly IOutboxMessageRepository _outboxRepository;

    // Coordinates saving changes to the database.
    private readonly IUnitOfWork _unitOfWork;

    // Executes database operations within transactions.
    private readonly ITransactionManager _transactionManager;

    // Records image storage and deletion errors.
    private readonly ILogger<ProductImageService> _logger;

    // Initializes the product image service with its required dependencies.
    public ProductImageService(
        IProductRepository productRepository,
        IRepository<ProductImage> imageRepository,
        ICloudinaryImageStorage storage,
        IOutboxMessageRepository outboxRepository,
        IUnitOfWork unitOfWork,
        ITransactionManager transactionManager,
        ILogger<ProductImageService> logger)
    {
        _productRepository = productRepository;
        _imageRepository = imageRepository;
        _storage = storage;
        _outboxRepository = outboxRepository;
        _unitOfWork = unitOfWork;
        _transactionManager = transactionManager;
        _logger = logger;
    }

    // Uploads new images for a product, optionally selects a new primary image, and invalidates the product cache.
    public async Task<IReadOnlyList<ProductImageResponse>> AddImagesAsync(
        int productId,
        IReadOnlyCollection<ProductImageUpload> newImages,
        int? primaryNewImageIndex,
        CancellationToken cancellationToken)
    {
        ValidateProductId(productId);
        ValidateFiles(newImages);

        if (newImages.Count == 0)
        {
            throw new ArgumentException(
                "At least one image is required.",
                nameof(newImages));
        }

        ValidatePrimaryNewIndex(
            primaryNewImageIndex,
            newImages.Count);

        var uploaded = new List<CloudinaryImageResult>();
        var databaseCommitted = false;

        try
        {
            // Uploads the image files to Cloudinary before starting the database transaction.
            foreach (var image in newImages)
            {
                cancellationToken.ThrowIfCancellationRequested();

                uploaded.Add(await _storage.UploadAsync(
                    image.Content,
                    Path.GetFileName(image.FileName),
                    cancellationToken));
            }

            var result =
                await _transactionManager.ExecuteAsync<
                    IReadOnlyList<ProductImageResponse>>(
                    async token =>
                    {
                        // Retrieves the product and its images for update.
                        var product =
                            await _productRepository
                                .GetByIdWithDetailsForUpdateAsync(
                                    productId,
                                    token);

                        if (product is null)
                        {
                            throw new KeyNotFoundException(
                                "Product was not found.");
                        }

                        var currentImages = product.Images.ToList();

                        // Ensures the product does not exceed its maximum image count.
                        if (currentImages.Count + uploaded.Count >
                            MaximumImagesPerProduct)
                        {
                            throw new InvalidOperationException(
                                $"A product cannot have more than {MaximumImagesPerProduct} images.");
                        }

                        var oldPrimary = currentImages
                            .FirstOrDefault(image => image.IsPrimary);

                        // Uses the requested primary image index or selects the first image when no primary exists.
                        var selectedPrimaryIndex =
                            primaryNewImageIndex ??
                            (oldPrimary is null ? 0 : -1);

                        if (selectedPrimaryIndex >= 0)
                        {
                            // Clears the current primary image when a new primary image is selected.
                            foreach (var image in currentImages)
                            {
                                image.SetPrimary(false);
                            }

                            await _unitOfWork.SaveChangesAsync(token);
                        }

                        // Determines the display order for the newly added images.
                        var nextOrder = currentImages.Count == 0
                            ? 0
                            : currentImages.Max(
                                image => image.DisplayOrder) + 1;

                        // Creates product image entities using the uploaded Cloudinary image details.
                        for (var index = 0;
                             index < uploaded.Count;
                             index++)
                        {
                            var cloudImage = uploaded[index];

                            var image = new ProductImage(
                                product.Id,
                                cloudImage.ImageUrl,
                                cloudImage.PublicId,
                                index == selectedPrimaryIndex,
                                nextOrder + index);

                            product.AddImage(image);
                        }

                        // Schedules product cache invalidation through the outbox.
                        AddInvalidation(productId);

                        await _unitOfWork.SaveChangesAsync(token);

                        return MapImages(product.Images);
                    },
                    IsolationLevel.ReadCommitted,
                    cancellationToken);

            databaseCommitted = true;

            return result;
        }
        catch
        {
            // Removes uploaded Cloudinary images if the database operation did not commit.
            if (!databaseCommitted)
            {
                await CleanupUploadedImagesAsync(uploaded);
            }

            throw;
        }
    }

    // Updates a product's image collection by retaining, adding, and removing images and selecting a primary image.
    public async Task<IReadOnlyList<ProductImageResponse>> UpdateImagesAsync(
        int productId,
        IReadOnlyCollection<int> keepImageIds,
        IReadOnlyCollection<ProductImageUpload> newImages,
        int? primaryImageId,
        int? primaryNewImageIndex,
        CancellationToken cancellationToken)
    {
        ValidateProductId(productId);

        ArgumentNullException.ThrowIfNull(keepImageIds);
        ValidateFiles(newImages);

        // Ensures all retained image IDs are positive.
        if (keepImageIds.Any(id => id <= 0))
        {
            throw new ArgumentException(
                "Image IDs must be positive.",
                nameof(keepImageIds));
        }

        // Prevents duplicate image IDs in the retained image collection.
        if (keepImageIds.Distinct().Count() != keepImageIds.Count)
        {
            throw new ArgumentException(
                "Duplicate image IDs are not allowed.",
                nameof(keepImageIds));
        }

        // Validates the ID of the requested existing primary image.
        if (primaryImageId.HasValue && primaryImageId.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(primaryImageId));
        }

        // Ensures that only one primary image selection method is used.
        if (primaryImageId.HasValue &&
            primaryNewImageIndex.HasValue)
        {
            throw new ArgumentException(
                "Choose either an existing primary image or a new primary image.");
        }

        ValidatePrimaryNewIndex(
            primaryNewImageIndex,
            newImages.Count);

        // Checks the combined number of retained and newly uploaded images.
        if (keepImageIds.Count + newImages.Count >
            MaximumImagesPerProduct)
        {
            throw new InvalidOperationException(
                $"A product cannot have more than {MaximumImagesPerProduct} images.");
        }

        var uploaded = new List<CloudinaryImageResult>();
        var removedPublicIds = new List<string>();
        var databaseCommitted = false;

        try
        {
            // Uploads new images to Cloudinary before updating the database.
            foreach (var image in newImages)
            {
                cancellationToken.ThrowIfCancellationRequested();

                uploaded.Add(await _storage.UploadAsync(
                    image.Content,
                    Path.GetFileName(image.FileName),
                    cancellationToken));
            }

            var result =
                await _transactionManager.ExecuteAsync<
                    IReadOnlyList<ProductImageResponse>>(
                    async token =>
                    {
                        // Retrieves the product and its images for update.
                        var product =
                            await _productRepository
                                .GetByIdWithDetailsForUpdateAsync(
                                    productId,
                                    token);

                        if (product is null)
                        {
                            throw new KeyNotFoundException(
                                "Product was not found.");
                        }

                        // Orders the existing images by display order and ID.
                        var currentImages = product.Images
                            .OrderBy(image => image.DisplayOrder)
                            .ThenBy(image => image.Id)
                            .ToList();

                        // Ensures all retained image IDs belong to this product.
                        if (keepImageIds.Any(
                            id => currentImages.All(
                                image => image.Id != id)))
                        {
                            throw new ArgumentException(
                                "One or more image IDs do not belong to this product.");
                        }

                        // Revalidates the total image count using the uploaded results.
                        if (keepImageIds.Count + uploaded.Count >
                            MaximumImagesPerProduct)
                        {
                            throw new InvalidOperationException(
                                $"A product cannot have more than {MaximumImagesPerProduct} images.");
                        }

                        // Ensures the selected existing primary image is retained.
                        if (primaryImageId.HasValue &&
                            keepImageIds.All(
                                id => id != primaryImageId.Value))
                        {
                            throw new ArgumentException(
                                "The primary image must be included in the retained image IDs.");
                        }

                        // Separates the images that will be retained from those that will be removed.
                        var retainedImages = currentImages
                            .Where(image => keepImageIds.Contains(image.Id))
                            .ToList();

                        var removedImages = currentImages
                            .Where(image => !keepImageIds.Contains(image.Id))
                            .ToList();

                        var oldPrimaryId = currentImages
                            .FirstOrDefault(image => image.IsPrimary)
                            ?.Id;

                        int? chosenPrimaryId = primaryImageId;

                        // Preserves the previous primary image when possible, otherwise selects the first retained image.
                        if (!primaryImageId.HasValue &&
                            !primaryNewImageIndex.HasValue)
                        {
                            chosenPrimaryId =
                                oldPrimaryId.HasValue &&
                                retainedImages.Any(
                                    image => image.Id == oldPrimaryId.Value)
                                    ? oldPrimaryId.Value
                                    : retainedImages.FirstOrDefault()?.Id;
                        }

                        // Clears the existing primary status before choosing the new primary image.
                        foreach (var image in currentImages)
                        {
                            image.SetPrimary(false);
                        }

                        await _unitOfWork.SaveChangesAsync(token);

                        // Removes unwanted images from the product and records their Cloudinary public IDs for deletion.
                        foreach (var image in removedImages)
                        {
                            product.RemoveImage(image);
                            _imageRepository.Delete(image);
                            removedPublicIds.Add(image.PublicId);
                        }

                        // Reassigns display order to the retained images.
                        for (var index = 0;
                             index < retainedImages.Count;
                             index++)
                        {
                            retainedImages[index].SetDisplayOrder(index);
                        }

                        var newEntities = new List<ProductImage>();

                        // Creates entities for newly uploaded images.
                        for (var index = 0;
                             index < uploaded.Count;
                             index++)
                        {
                            var cloudImage = uploaded[index];

                            var image = new ProductImage(
                                product.Id,
                                cloudImage.ImageUrl,
                                cloudImage.PublicId,
                                false,
                                retainedImages.Count + index);

                            product.AddImage(image);
                            newEntities.Add(image);
                        }

                        // Combines retained and newly created images into the final collection.
                        var finalImages = retainedImages
                            .Concat(newEntities)
                            .ToList();

                        ProductImage? selectedPrimary = null;

                        // Selects the requested existing image as the primary image.
                        if (primaryImageId.HasValue)
                        {
                            selectedPrimary = retainedImages.Single(
                                image => image.Id == primaryImageId.Value);
                        }
                        // Selects a newly uploaded image as the primary image when requested.
                        else if (primaryNewImageIndex.HasValue)
                        {
                            selectedPrimary =
                                newEntities[primaryNewImageIndex.Value];
                        }
                        // Preserves the chosen existing primary image when possible.
                        else if (chosenPrimaryId.HasValue)
                        {
                            selectedPrimary = retainedImages.First(
                                image => image.Id == chosenPrimaryId.Value);
                        }
                        // Selects the first available image when no primary image was chosen.
                        else
                        {
                            selectedPrimary = finalImages.FirstOrDefault();
                        }

                        selectedPrimary?.SetPrimary(true);

                        // Normalizes the display order of the final image collection.
                        NormalizeDisplayOrder(finalImages);

                        // Schedules product cache invalidation through the outbox.
                        AddInvalidation(productId);

                        await _unitOfWork.SaveChangesAsync(token);

                        return MapImages(finalImages);
                    },
                    IsolationLevel.ReadCommitted,
                    cancellationToken);

            databaseCommitted = true;

            // Deletes removed images from Cloudinary after the database changes have committed.
            foreach (var publicId in removedPublicIds)
            {
                await TryDeleteCloudinaryAsync(
                    publicId,
                    cancellationToken);
            }

            return result;
        }
        catch
        {
            // Removes newly uploaded images if the database changes did not commit.
            if (!databaseCommitted)
            {
                await CleanupUploadedImagesAsync(uploaded);
            }

            throw;
        }
    }

    // Deletes a product image from the database and Cloudinary, selecting a replacement primary image when necessary.
    public async Task DeleteImageAsync(
        int productId,
        int imageId,
        CancellationToken cancellationToken)
    {
        ValidateProductId(productId);

        if (imageId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(imageId));
        }

        string? publicId = null;

        await _transactionManager.ExecuteAsync<bool>(
            async token =>
            {
                // Retrieves the product and its images for update.
                var product =
                    await _productRepository
                        .GetByIdWithDetailsForUpdateAsync(
                            productId,
                            token);

                if (product is null)
                {
                    throw new KeyNotFoundException(
                        "Product was not found.");
                }

                // Finds the requested image within the specified product.
                var image = product.Images
                    .FirstOrDefault(item => item.Id == imageId);

                if (image is null)
                {
                    throw new KeyNotFoundException(
                        "Image was not found for this product.");
                }

                publicId = image.PublicId;

                var wasPrimary = image.IsPrimary;

                // Removes the image from the product and marks it for database deletion.
                product.RemoveImage(image);
                _imageRepository.Delete(image);

                // Retrieves the remaining images in display order.
                var remainingImages = product.Images
                    .OrderBy(item => item.DisplayOrder)
                    .ThenBy(item => item.Id)
                    .ToList();

                if (wasPrimary)
                {
                    // Clears primary status from all remaining images before selecting a replacement.
                    foreach (var remainingImage in remainingImages)
                    {
                        remainingImage.SetPrimary(false);
                    }

                    await _unitOfWork.SaveChangesAsync(token);

                    // Assigns primary status to the first remaining image, if one exists.
                    remainingImages
                        .FirstOrDefault()
                        ?.SetPrimary(true);
                }

                NormalizeDisplayOrder(remainingImages);

                // Schedules product cache invalidation through the outbox.
                AddInvalidation(productId);

                await _unitOfWork.SaveChangesAsync(token);

                return true;
            },
            IsolationLevel.ReadCommitted,
            cancellationToken);

        // Deletes the corresponding Cloudinary asset after the database transaction succeeds.
        if (publicId is not null)
        {
            await TryDeleteCloudinaryAsync(
                publicId,
                cancellationToken);
        }
    }

    // Creates an outbox message requesting invalidation of the specified product's cache.
    private void AddInvalidation(int productId)
    {
        var payload = JsonSerializer.Serialize(
            new ProductCacheInvalidationRequested(productId));

        _outboxRepository.Add(
            new OutboxMessage(
                typeof(ProductCacheInvalidationRequested).FullName!,
                payload));
    }

    // Attempts to delete uploaded Cloudinary images when a database operation fails.
    private async Task CleanupUploadedImagesAsync(
        IEnumerable<CloudinaryImageResult> uploaded)
    {
        foreach (var image in uploaded)
        {
            await TryDeleteCloudinaryAsync(
                image.PublicId,
                CancellationToken.None);
        }
    }

    // Attempts to delete an image from Cloudinary and logs an error if deletion fails.
    private async Task TryDeleteCloudinaryAsync(
        string publicId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _storage.DeleteAsync(
                publicId,
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Could not delete Cloudinary image {PublicId}.",
                publicId);
        }
    }

    // Maps product image entities to response DTOs ordered by primary status, display order, and ID.
    private static IReadOnlyList<ProductImageResponse> MapImages(
        IEnumerable<ProductImage> images)
    {
        return images
            .OrderByDescending(image => image.IsPrimary)
            .ThenBy(image => image.DisplayOrder)
            .ThenBy(image => image.Id)
            .Select(image => new ProductImageResponse
            {
                Id = image.Id,
                ProductId = image.ProductId,
                ImageUrl = image.ImageUrl,
                IsPrimary = image.IsPrimary,
                DisplayOrder = image.DisplayOrder
            })
            .ToList();
    }

    // Reassigns sequential display order values to the supplied images.
    private static void NormalizeDisplayOrder(
        IEnumerable<ProductImage> images)
    {
        var orderedImages = images
            .OrderBy(image => image.DisplayOrder)
            .ThenBy(image => image.Id)
            .ToList();

        for (var index = 0; index < orderedImages.Count; index++)
        {
            orderedImages[index].SetDisplayOrder(index);
        }
    }

    // Validates that the product ID is a positive integer.
    private static void ValidateProductId(int productId)
    {
        if (productId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(productId));
        }
    }

    // Validates uploaded image files against size, readability, extension, and content type requirements.
    private static void ValidateFiles(
        IReadOnlyCollection<ProductImageUpload> images)
    {
        ArgumentNullException.ThrowIfNull(images);

        if (images.Count > MaximumImagesPerProduct)
        {
            throw new ArgumentException(
                $"A maximum of {MaximumImagesPerProduct} images can be uploaded at once.",
                nameof(images));
        }

        foreach (var image in images)
        {
            if (image is null)
            {
                throw new ArgumentException(
                    "The image collection cannot contain null items.",
                    nameof(images));
            }

            if (image.Content is null || !image.Content.CanRead)
            {
                throw new ArgumentException(
                    "One of the image streams cannot be read.");
            }

            if (image.Length <= 0 || image.Length > MaximumFileSize)
            {
                throw new ArgumentException(
                    "Each image must be larger than zero and no larger than 5 MB.");
            }

            var extension = Path.GetExtension(image.FileName);

            if (!AllowedExtensions.Contains(extension) ||
                !AllowedContentTypes.Contains(image.ContentType))
            {
                throw new ArgumentException(
                    "Only JPG, JPEG, PNG and WebP images are allowed.");
            }
        }
    }

    // Validates that the selected primary image index falls within the range of uploaded files.
    private static void ValidatePrimaryNewIndex(
        int? primaryNewImageIndex,
        int newImagesCount)
    {
        if (!primaryNewImageIndex.HasValue)
        {
            return;
        }

        if (primaryNewImageIndex.Value < 0 ||
            primaryNewImageIndex.Value >= newImagesCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(primaryNewImageIndex),
                "The primary new image index is outside the uploaded files range.");
        }
    }
}