using Feryal.Application.DTOs.Common;
using Feryal.Application.DTOs.Products;
using Feryal.Application.DTOs.Products.Response;
using Feryal.Application.Interfaces;
using Feryal.Application.Products.Images;
using Feryal.Application.Products.Interfaces;
using Feryal.Domain.Entities;
using Microsoft.Extensions.Caching.Hybrid;

namespace Feryal.Infrastructure.Caching.Products;

public sealed class ProductCacheService : IProductCacheService
{
    private static readonly HybridCacheEntryOptions CacheOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(5)
    };

    private readonly HybridCache _cache;
    private readonly IProductRepository _products;

    public ProductCacheService(
        HybridCache cache,
        IProductRepository products)
    {
        _cache = cache;
        _products = products;
    }


    public async ValueTask<PagedResponse<ProductListResponse>> GetAllAsync(
        ProductQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var key = ProductCacheConstants.GetListKey(parameters);

        return await _cache.GetOrCreateAsync(
            key,
            async token =>
            {
                var result = await _products.GetPagedAsync(
                    parameters,
                    token);

                var items = result.Items
                    .Select(MapListItem)
                    .ToList();

                return new PagedResponse<ProductListResponse>
                {
                    Items = items,
                    PageNumber = parameters.PageNumber,
                    PageSize = parameters.PageSize,
                    TotalCount = result.TotalCount
                };
            },
            CacheOptions,
            new[]
            {
            ProductCacheConstants.AllProductsTag,
            ProductCacheConstants.ProductListsTag
            },
            cancellationToken);
    }


    public async ValueTask<ProductResponse?> GetByIdAsync(
        int productId,
        CancellationToken cancellationToken)
    {
        var key = ProductCacheConstants.GetDetailsKey(productId);

        return await _cache.GetOrCreateAsync<ProductResponse?>(
            key,
            async token =>
            {
                var product = await _products.GetByIdWithDetailsAsync(
                    productId,
                    token);

                return product is null ? null : MapProductDetails(product);
            },
            CacheOptions,
            new[] { ProductCacheConstants.GetProductTag(productId) },
            cancellationToken);
    }

    public async ValueTask InvalidateListsAsync(
        CancellationToken cancellationToken)
    {
        await _cache.RemoveByTagAsync(
            ProductCacheConstants.ProductListsTag,
            cancellationToken);
    }

    public async ValueTask InvalidateProductAsync(
        int productId,
        CancellationToken cancellationToken)
    {
        await _cache.RemoveByTagAsync(
            ProductCacheConstants.GetProductTag(productId),
            cancellationToken);

        await InvalidateListsAsync(cancellationToken);
    }

    private static ProductListResponse MapListItem(Product product)
    {
        var primaryImage = product.Images
            .Where(image => image.IsPrimary)
            .Select(image => image.ImageUrl)
            .FirstOrDefault();

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

            Sizes = product.Sizes
                .OrderBy(item => item.Id)
                .Select(item => new ProductSizeResponse
                {
                    Id = item.Id,
                    Name = item.Name,
                    IsActive = item.IsActive
                })
                .ToList(),

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