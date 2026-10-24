using Feryal.Application.DTOs.Categories;
using Feryal.Application.DTOs.Common;
using Feryal.Application.Interfaces;
using Feryal.Infrastructure.Caching.Categories;
using Feryal.Domain.Entities;
using Microsoft.Extensions.Caching.Hybrid;

namespace Feryal.Infrastructure.Caching.Categories;

// Provides hybrid caching for category lists and individual category details.
public sealed class CategoryCacheService : ICategoryCacheService
{
    // Handles cache storage and retrieval across local and distributed cache layers.
    private readonly HybridCache _hybridCache;

    // Retrieves category data from the database when the cache misses.
    private readonly ICategoryRepository _categoryRepository;

    // Defines the expiration periods for distributed and local cache entries.
    private static readonly HybridCacheEntryOptions CacheOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(5),
        LocalCacheExpiration = TimeSpan.FromMinutes(1)
    };

    // Initializes the service with the hybrid cache and category repository.
    public CategoryCacheService(
        HybridCache hybridCache,
        ICategoryRepository categoryRepository)
    {
        _hybridCache = hybridCache;
        _categoryRepository = categoryRepository;
    }

    // Retrieves a paginated category list from the cache or repository.
    public async ValueTask<PagedResponse<CategoryResponse>> GetAllAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        // Generate a cache key based on the requested pagination settings.
        var cacheKey = CategoryCacheConstants.GetListKey(
            pageNumber,
            pageSize);

        // Return the cached response or create it by fetching data from the repository.
        return await _hybridCache.GetOrCreateAsync(
            cacheKey,
            async token =>
            {
                // Retrieve the requested page of categories and its total count.
                var result = await _categoryRepository.GetPagedAsync(
                    pageNumber,
                    pageSize,
                    token);

                // Convert category entities into response DTOs.
                var items = result.Items
                    .Select(MapToResponse)
                    .ToList();

                // Build the paginated response using the retrieved category data.
                return new PagedResponse<CategoryResponse>
                {
                    Items = items,
                    TotalCount = result.TotalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };
            },
            options: CacheOptions,
            // Associate the entry with tags for global and category-list invalidation.
            tags:
            [
                CategoryCacheConstants.AllCategoriesTag,
                CategoryCacheConstants.CategoryListsTag
            ],
            cancellationToken: cancellationToken);
    }

    // Retrieves a category by ID from the cache or repository.
    public async ValueTask<CategoryResponse?> GetByIdAsync(
        int categoryId,
        CancellationToken cancellationToken)
    {
        // Generate a unique cache key for the requested category.
        var cacheKey = CategoryCacheConstants.GetDetailsKey(categoryId);

        // Return the cached category or retrieve it from the repository on a cache miss.
        return await _hybridCache.GetOrCreateAsync<CategoryResponse?>(
            cacheKey,
            async token =>
            {
                // Retrieve the category entity by its ID.
                var category = await _categoryRepository.GetByIdAsync(
                    categoryId,
                    token);

                // Return null if the category does not exist; otherwise, map it to a DTO.
                return category is null
                    ? null
                    : MapToResponse(category);
            },
            options: CacheOptions,
            // Associate the entry with global and category-specific invalidation tags.
            tags:
            [
                CategoryCacheConstants.AllCategoriesTag,
                CategoryCacheConstants.GetCategoryTag(categoryId)
            ],
            cancellationToken: cancellationToken);
    }

    // Invalidates all cached category list entries.
    public async ValueTask InvalidateListsAsync(
        CancellationToken cancellationToken)
    {
        // Remove cache entries associated with the category lists tag.
        await _hybridCache.RemoveByTagAsync(
            CategoryCacheConstants.CategoryListsTag,
            cancellationToken);
    }

    // Invalidates the specified category's details and all cached category lists.
    public async ValueTask InvalidateCategoryAsync(
        int categoryId,
        CancellationToken cancellationToken)
    {
        // Remove cache entries associated with the specified category.
        await _hybridCache.RemoveByTagAsync(
            CategoryCacheConstants.GetCategoryTag(categoryId),
            cancellationToken);

        // Invalidate category lists because the category data may affect them.
        await InvalidateListsAsync(cancellationToken);
    }

    // Maps a category entity to the response DTO returned by the API.
    private static CategoryResponse MapToResponse(Category category)
    {
        return new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            IsActive = category.IsActive,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };
    }
}