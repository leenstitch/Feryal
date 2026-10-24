using Feryal.Application.DTOs.Common;
using Feryal.Application.DTOs.ProductGroups;
using Feryal.Application.Interfaces;
using Feryal.Domain.Entities;
using Microsoft.Extensions.Caching.Hybrid;

namespace Feryal.Infrastructure.Caching.ProductGroups;

// Provides hybrid caching for product group lists and individual product group details.
public sealed class ProductGroupCacheService : IProductGroupCacheService
{
    // Handles cache storage, retrieval, and invalidation.
    private readonly HybridCache _hybridCache;

    // Retrieves product group data from the repository when the cache misses.
    private readonly IProductGroupRepository _productGroupRepository;

    // Defines the expiration periods for distributed and local cache entries.
    private static readonly HybridCacheEntryOptions CacheOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(5),
        LocalCacheExpiration = TimeSpan.FromMinutes(1)
    };

    // Initializes the service with the hybrid cache and product group repository.
    public ProductGroupCacheService(
        HybridCache hybridCache,
        IProductGroupRepository productGroupRepository)
    {
        _hybridCache = hybridCache;
        _productGroupRepository = productGroupRepository;
    }

    // Retrieves a paginated list of product groups from the cache or repository.
    public async ValueTask<PagedResponse<ProductGroupResponse>> GetAllAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        // Generate a cache key based on the requested pagination settings.
        var cacheKey = ProductGroupCacheConstants.GetListKey(
            pageNumber,
            pageSize);

        // Return the cached response or create it by retrieving data from the repository.
        return await _hybridCache.GetOrCreateAsync(
            cacheKey,
            async token =>
            {
                // Retrieve the requested page of product groups and the total count.
                var result = await _productGroupRepository.GetPagedAsync(
                    pageNumber,
                    pageSize,
                    token);

                // Convert product group entities into response DTOs.
                var items = result.Items
                    .Select(MapToResponse)
                    .ToList();

                // Build the paginated response with its items and metadata.
                return new PagedResponse<ProductGroupResponse>
                {
                    Items = items,
                    TotalCount = result.TotalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };
            },
            options: CacheOptions,
            // Tag the entry so all product group list caches can be invalidated together.
            tags: [ProductGroupCacheConstants.ProductGroupListsTag],
            cancellationToken: cancellationToken);
    }

    // Retrieves a product group by ID from the cache or repository.
    public async ValueTask<ProductGroupResponse?> GetByIdAsync(
        int productGroupId,
        CancellationToken cancellationToken)
    {
        // Generate a unique cache key for the requested product group.
        var cacheKey = ProductGroupCacheConstants.GetDetailsKey(
            productGroupId);

        // Return the cached product group or retrieve it from the repository on a cache miss.
        return await _hybridCache.GetOrCreateAsync<ProductGroupResponse?>(
            cacheKey,
            async token =>
            {
                // Retrieve the product group entity by its ID.
                var productGroup = await _productGroupRepository.GetByIdAsync(
                    productGroupId,
                    token);

                // Return null if the product group does not exist; otherwise, map it to a DTO.
                return productGroup is null
                    ? null
                    : MapToResponse(productGroup);
            },
            options: CacheOptions,
            // Associate the entry with a tag for product group-specific invalidation.
            tags:
            [
                ProductGroupCacheConstants.GetProductGroupTag(productGroupId)
            ],
            cancellationToken: cancellationToken);
    }

    // Invalidates all cached product group list entries.
    public async ValueTask InvalidateListsAsync(
        CancellationToken cancellationToken)
    {
        // Remove cache entries associated with the product group lists tag.
        await _hybridCache.RemoveByTagAsync(
            ProductGroupCacheConstants.ProductGroupListsTag,
            cancellationToken);
    }

    // Invalidates the specified product group's details and all product group lists.
    public async ValueTask InvalidateProductGroupAsync(
        int productGroupId,
        CancellationToken cancellationToken)
    {
        // Remove cache entries associated with the specified product group.
        await _hybridCache.RemoveByTagAsync(
            ProductGroupCacheConstants.GetProductGroupTag(productGroupId),
            cancellationToken);

        // Invalidate lists because the product group's data may affect their results.
        await InvalidateListsAsync(cancellationToken);
    }

    // Maps a product group entity to the response DTO returned by the API.
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