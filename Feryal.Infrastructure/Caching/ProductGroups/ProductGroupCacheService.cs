
using Feryal.Application.DTOs.Common;
using Feryal.Application.DTOs.ProductGroups;
using Feryal.Application.Interfaces;
using Feryal.Domain.Entities;
using Microsoft.Extensions.Caching.Hybrid;

namespace Feryal.Infrastructure.Caching.ProductGroups;

public sealed class ProductGroupCacheService : IProductGroupCacheService
{
    private readonly HybridCache _hybridCache;
    private readonly IProductGroupRepository _productGroupRepository;

    private static readonly HybridCacheEntryOptions CacheOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(5),
        LocalCacheExpiration = TimeSpan.FromMinutes(1)
    };

    public ProductGroupCacheService(
        HybridCache hybridCache,
        IProductGroupRepository productGroupRepository)
    {
        _hybridCache = hybridCache;
        _productGroupRepository = productGroupRepository;
    }

    public async ValueTask<PagedResponse<ProductGroupResponse>> GetAllAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var cacheKey = ProductGroupCacheConstants.GetListKey(
            pageNumber,
            pageSize);

        return await _hybridCache.GetOrCreateAsync(
            cacheKey,
            async token =>
            {
                var result = await _productGroupRepository.GetPagedAsync(
                    pageNumber,
                    pageSize,
                    token);

                var items = result.Items
                    .Select(MapToResponse)
                    .ToList();

                return new PagedResponse<ProductGroupResponse>
                {
                    Items = items,
                    TotalCount = result.TotalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };
            },
            options: CacheOptions,
            tags: [ProductGroupCacheConstants.ProductGroupListsTag],
            cancellationToken: cancellationToken);
    }

    public async ValueTask<ProductGroupResponse?> GetByIdAsync(
        int productGroupId,
        CancellationToken cancellationToken)
    {
        var cacheKey = ProductGroupCacheConstants.GetDetailsKey(
            productGroupId);

        return await _hybridCache.GetOrCreateAsync<ProductGroupResponse?>(
            cacheKey,
            async token =>
            {
                var productGroup = await _productGroupRepository.GetByIdAsync(
                    productGroupId,
                    token);

                return productGroup is null
                    ? null
                    : MapToResponse(productGroup);
            },
            options: CacheOptions,
            tags:
            [
                ProductGroupCacheConstants.GetProductGroupTag(productGroupId)
            ],
            cancellationToken: cancellationToken);
    }

    public async ValueTask InvalidateListsAsync(
        CancellationToken cancellationToken)
    {
        await _hybridCache.RemoveByTagAsync(
            ProductGroupCacheConstants.ProductGroupListsTag,
            cancellationToken);
    }

    public async ValueTask InvalidateProductGroupAsync(
        int productGroupId,
        CancellationToken cancellationToken)
    {
        await _hybridCache.RemoveByTagAsync(
            ProductGroupCacheConstants.GetProductGroupTag(productGroupId),
            cancellationToken);

        await InvalidateListsAsync(cancellationToken);
    }

    private static ProductGroupResponse MapToResponse(
        ProductGroup productGroup) => new()
        {
            Id = productGroup.Id,
            Name = productGroup.Name,
            Description = productGroup.Description,
            IsActive = productGroup.IsActive,
            CreatedAt = productGroup.CreatedAt,
            UpdatedAt = productGroup.UpdatedAt
        };
}