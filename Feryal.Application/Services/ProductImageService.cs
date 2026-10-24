
using System.Data;
using System.Text.Json;
using Feryal.Application.Events.Products;
using Feryal.Application.Interfaces;
using Feryal.Application.Products.Images;
using Feryal.Application.Products.Interfaces;
using Feryal.Domain.Entities;
using Feryal.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Feryal.Infrastructure.Products;

public sealed class ProductImageService : IProductImageService
{
    private const int MaximumImagesPerProduct = 20;
    private const long MaximumFileSize = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

    private static readonly HashSet<string> AllowedContentTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

    private readonly IProductRepository _productRepository;
    private readonly IRepository<ProductImage> _imageRepository;
    private readonly ICloudinaryImageStorage _storage;
    private readonly IOutboxMessageRepository _outboxRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITransactionManager _transactionManager;
    private readonly ILogger<ProductImageService> _logger;

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

                        if (currentImages.Count + uploaded.Count >
                            MaximumImagesPerProduct)
                        {
                            throw new InvalidOperationException(
                                $"A product cannot have more than {MaximumImagesPerProduct} images.");
                        }

                        var oldPrimary = currentImages
                            .FirstOrDefault(image => image.IsPrimary);

                        var selectedPrimaryIndex =
                            primaryNewImageIndex ??
                            (oldPrimary is null ? 0 : -1);

                        if (selectedPrimaryIndex >= 0)
                        {
                            foreach (var image in currentImages)
                            {
                                image.SetPrimary(false);
                            }

                            await _unitOfWork.SaveChangesAsync(token);
                        }

                        var nextOrder = currentImages.Count == 0
                            ? 0
                            : currentImages.Max(
                                image => image.DisplayOrder) + 1;

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
            if (!databaseCommitted)
            {
                await CleanupUploadedImagesAsync(uploaded);
            }

            throw;
        }
    }

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

        if (keepImageIds.Any(id => id <= 0))
        {
            throw new ArgumentException(
                "Image IDs must be positive.",
                nameof(keepImageIds));
        }

        if (keepImageIds.Distinct().Count() != keepImageIds.Count)
        {
            throw new ArgumentException(
                "Duplicate image IDs are not allowed.",
                nameof(keepImageIds));
        }

        if (primaryImageId.HasValue && primaryImageId.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(primaryImageId));
        }

        if (primaryImageId.HasValue &&
            primaryNewImageIndex.HasValue)
        {
            throw new ArgumentException(
                "Choose either an existing primary image or a new primary image.");
        }

        ValidatePrimaryNewIndex(
            primaryNewImageIndex,
            newImages.Count);

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

                        var currentImages = product.Images
                            .OrderBy(image => image.DisplayOrder)
                            .ThenBy(image => image.Id)
                            .ToList();

                        if (keepImageIds.Any(
                            id => currentImages.All(
                                image => image.Id != id)))
                        {
                            throw new ArgumentException(
                                "One or more image IDs do not belong to this product.");
                        }

                        if (keepImageIds.Count + uploaded.Count >
                            MaximumImagesPerProduct)
                        {
                            throw new InvalidOperationException(
                                $"A product cannot have more than {MaximumImagesPerProduct} images.");
                        }

                        if (primaryImageId.HasValue &&
                            keepImageIds.All(
                                id => id != primaryImageId.Value))
                        {
                            throw new ArgumentException(
                                "The primary image must be included in the retained image IDs.");
                        }

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

                        // Clear the existing primary before selecting another.
                        foreach (var image in currentImages)
                        {
                            image.SetPrimary(false);
                        }

                        await _unitOfWork.SaveChangesAsync(token);

                        foreach (var image in removedImages)
                        {
                            product.RemoveImage(image);
                            _imageRepository.Delete(image);
                            removedPublicIds.Add(image.PublicId);
                        }

                        for (var index = 0;
                             index < retainedImages.Count;
                             index++)
                        {
                            retainedImages[index].SetDisplayOrder(index);
                        }

                        var newEntities = new List<ProductImage>();

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

                        var finalImages = retainedImages
                            .Concat(newEntities)
                            .ToList();

                        ProductImage? selectedPrimary = null;

                        if (primaryImageId.HasValue)
                        {
                            selectedPrimary = retainedImages.Single(
                                image => image.Id == primaryImageId.Value);
                        }
                        else if (primaryNewImageIndex.HasValue)
                        {
                            selectedPrimary =
                                newEntities[primaryNewImageIndex.Value];
                        }
                        else if (chosenPrimaryId.HasValue)
                        {
                            selectedPrimary = retainedImages.First(
                                image => image.Id == chosenPrimaryId.Value);
                        }
                        else
                        {
                            selectedPrimary = finalImages.FirstOrDefault();
                        }

                        selectedPrimary?.SetPrimary(true);

                        NormalizeDisplayOrder(finalImages);

                        AddInvalidation(productId);

                        await _unitOfWork.SaveChangesAsync(token);

                        return MapImages(finalImages);
                    },
                    IsolationLevel.ReadCommitted,
                    cancellationToken);

            databaseCommitted = true;

            // Database changes succeeded; now remove deleted assets from Cloudinary.
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
            if (!databaseCommitted)
            {
                await CleanupUploadedImagesAsync(uploaded);
            }

            throw;
        }
    }

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

                var image = product.Images
                    .FirstOrDefault(item => item.Id == imageId);

                if (image is null)
                {
                    throw new KeyNotFoundException(
                        "Image was not found for this product.");
                }

                publicId = image.PublicId;

                var wasPrimary = image.IsPrimary;

                product.RemoveImage(image);
                _imageRepository.Delete(image);

                var remainingImages = product.Images
                    .OrderBy(item => item.DisplayOrder)
                    .ThenBy(item => item.Id)
                    .ToList();

                if (wasPrimary)
                {
                    foreach (var remainingImage in remainingImages)
                    {
                        remainingImage.SetPrimary(false);
                    }

                    await _unitOfWork.SaveChangesAsync(token);

                    remainingImages
                        .FirstOrDefault()
                        ?.SetPrimary(true);
                }

                NormalizeDisplayOrder(remainingImages);

                AddInvalidation(productId);

                await _unitOfWork.SaveChangesAsync(token);

                return true;
            },
            IsolationLevel.ReadCommitted,
            cancellationToken);

        if (publicId is not null)
        {
            await TryDeleteCloudinaryAsync(
                publicId,
                cancellationToken);
        }
    }

    private void AddInvalidation(int productId)
    {
        var payload = JsonSerializer.Serialize(
            new ProductCacheInvalidationRequested(productId));

        _outboxRepository.Add(
            new OutboxMessage(
                typeof(ProductCacheInvalidationRequested).FullName!,
                payload));
    }

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

    private static void ValidateProductId(int productId)
    {
        if (productId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(productId));
        }
    }

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
