using Feryal.Application.DTOs.Common;
using Feryal.Application.DTOs.Products;
using Feryal.Application.DTOs.Products.Response;
using Feryal.Application.Interfaces;
using Feryal.Application.Products.Images;
using Feryal.Application.Products.Interfaces;
using Feryal.Domain.Entities;
using Microsoft.Extensions.Caching.Hybrid;

namespace Feryal.Infrastructure.Caching.Products;

// Provides hybrid caching for product lists and product details.
public sealed class ProductCacheService : IProductCacheService
{
    // Defines how long cached product entries remain valid.
    private static readonly HybridCacheEntryOptions CacheOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(5)
    };

    // Handles cache storage, retrieval, and invalidation.
    private readonly HybridCache _cache;

    // Retrieves product data from the repository when the cache misses.
    private readonly IProductRepository _products;

    // Initializes the service with the hybrid cache and product repository.
    public ProductCacheService(
        HybridCache cache,
        IProductRepository products)
    {
        _cache = cache;
        _products = products;
    }


    // Retrieves a paginated product list from the cache or repository.
    public async ValueTask<PagedResponse<ProductListResponse>> GetAllAsync(
        ProductQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        // Generate a cache key based on the product query parameters.
        var key = ProductCacheConstants.GetListKey(parameters);

        // Return the cached response or retrieve and cache the requested product list.
        return await _cache.GetOrCreateAsync(
            key,
            async token =>
            {
                // Retrieve products matching the pagination and filter parameters.
                var result = await _products.GetPagedAsync(
                    parameters,
                    token);

                // Convert product entities into list response DTOs.
                var items = result.Items
                    .Select(MapListItem)
                    .ToList();

                // Build the paginated response with the retrieved items and metadata.
                return new PagedResponse<ProductListResponse>
                {
                    Items = items,
                    PageNumber = parameters.PageNumber,
                    PageSize = parameters.PageSize,
                    TotalCount = result.TotalCount
                };
            },
            CacheOptions,
            // Tag entries for global product and product-list cache invalidation.
            new[]
            {
            ProductCacheConstants.AllProductsTag,
            ProductCacheConstants.ProductListsTag
            },
            cancellationToken);
    }


    // Retrieves detailed information for a specific product from the cache or repository.
    public async ValueTask<ProductResponse?> GetByIdAsync(
        int productId,
        CancellationToken cancellationToken)
    {
        // Generate the cache key for the requested product.
        var key = ProductCacheConstants.GetDetailsKey(productId);

        // Return the cached details or load the product from the repository on a cache miss.
        return await _cache.GetOrCreateAsync<ProductResponse?>(
            key,
            async token =>
            {
                // Retrieve the product together with its related details.
                var product = await _products.GetByIdWithDetailsAsync(
                    productId,
                    token);

                // Return null if the product does not exist; otherwise, map it to a response DTO.
                return product is null ? null : MapProductDetails(product);
            },
            CacheOptions,
            // Tag the entry so the cache for this product can be invalidated individually.
            new[] { ProductCacheConstants.GetProductTag(productId) },
            cancellationToken);
    }

    // Invalidates all cached product list entries.
    public async ValueTask InvalidateListsAsync(
        CancellationToken cancellationToken)
    {
        // Remove entries associated with the product lists tag.
        await _cache.RemoveByTagAsync(
            ProductCacheConstants.ProductListsTag,
            cancellationToken);
    }

    // Invalidates the specified product's details and all cached product lists.
    public async ValueTask InvalidateProductAsync(
        int productId,
        CancellationToken cancellationToken)
    {
        // Remove cache entries associated with the specified product.
        await _cache.RemoveByTagAsync(
            ProductCacheConstants.GetProductTag(productId),
            cancellationToken);

        // Invalidate product lists because the product's data may affect their results.
        await InvalidateListsAsync(cancellationToken);
    }

    // Maps a product entity to the response DTO used in product listings.
    private static ProductListResponse MapListItem(Product product)
    {
        // Find the URL of the product's primary image, if one exists.
        var primaryImage = product.Images
            .Where(image => image.IsPrimary)
            .Select(image => image.ImageUrl)
            .FirstOrDefault();

        // Return the product's general information and primary image.
        return new ProductListResponse
        {
            Id = product.Id,
            CategoryId = product.CategoryId,
            ProductGroupId = product.ProductGroupId,
            Name = product.Name,
            Description = product.Description,
            IsActive = product.IsActive,
            PrimaryImageUrl = primaryImage
        };
    }

    // Maps a product entity and its related data to the detailed product response DTO.
    private static ProductResponse MapProductDetails(Product product)
    {
        return new ProductResponse
        {
            Id = product.Id,
            CategoryId = product.CategoryId,
            ProductGroupId = product.ProductGroupId,
            Name = product.Name,
            Description = product.Description,
            IsActive = product.IsActive,

            // Map product colors in ascending ID order.
            Colors = product.Colors
                .OrderBy(item => item.Id)
                .Select(item => new ProductColorResponse
                {
                    Id = item.Id,
                    Name = item.Name,
                    HexCode = item.HexCode,
                    IsActive = item.IsActive
                })
                .ToList(),

            // Map product sizes in ascending ID order.
            Sizes = product.Sizes
                .OrderBy(item => item.Id)
                .Select(item => new ProductSizeResponse
                {
                    Id = item.Id,
                    Name = item.Name,
                    IsActive = item.IsActive
                })
                .ToList(),

            // Map product scents in ascending ID order.
            Scents = product.Scents
                .OrderBy(item => item.Id)
                .Select(item => new ProductScentResponse
                {
                    Id = item.Id,
                    Name = item.Name,
                    IsActive = item.IsActive
                })
                .ToList(),

            // Structural variant information only.
            // Price, discount and stock are never cached.
            // Map variant configuration and related color, size, and scent information.
            Variants = product.Variants
                .OrderBy(item => item.Id)
                .Select(item => new ProductVariantResponse
                {
                    Id = item.Id,
                    ProductId = item.ProductId,
                    ScentId = item.ScentId,
                    ScentName = item.Scent?.Name,
                    SizeId = item.SizeId,
                    SizeName = item.Size?.Name,
                    ColorId = item.ColorId,
                    ColorName = item.Color?.Name,
                    ColorHexCode = item.Color?.HexCode,
                    SKU = item.SKU,
                    Description = item.Description,
                    IsActive = item.IsActive,
                    IsConfigured = item.IsConfigured,
                    HasCompleteSellingDetails = item.HasCompleteSellingDetails
                })
                .ToList(),

            // Map product images, placing the primary image first and preserving display order.
            Images = product.Images
                .OrderByDescending(item => item.IsPrimary)
                .ThenBy(item => item.DisplayOrder)
                .Select(item => new ProductImageResponse
                {
                    Id = item.Id,
                    ImageUrl = item.ImageUrl,
                    IsPrimary = item.IsPrimary,
                    DisplayOrder = item.DisplayOrder
                })
                .ToList()
        };
    }
}