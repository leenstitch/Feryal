
using Feryal.Application.DTOs.Categories;
using Feryal.Application.DTOs.Common;
using Feryal.Application.Interfaces;
using Feryal.Infrastructure.Caching.Categories;
using Feryal.Domain.Entities;
using Microsoft.Extensions.Caching.Hybrid;

namespace Feryal.Infrastructure.Caching.Categories;

public sealed class CategoryCacheService : ICategoryCacheService
{
    private readonly HybridCache _hybridCache;
    private readonly ICategoryRepository _categoryRepository;

    private static readonly HybridCacheEntryOptions CacheOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(5),
        LocalCacheExpiration = TimeSpan.FromMinutes(1)
    };

    public CategoryCacheService(
        HybridCache hybridCache,
        ICategoryRepository categoryRepository)
    {
        _hybridCache = hybridCache;
        _categoryRepository = categoryRepository;
    }

    public async ValueTask<PagedResponse<CategoryResponse>> GetAllAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var cacheKey = CategoryCacheConstants.GetListKey(
            pageNumber,
            pageSize);

        return await _hybridCache.GetOrCreateAsync(
            cacheKey,
            async token =>
            {
                var result = await _categoryRepository.GetPagedAsync(
                    pageNumber,
                    pageSize,
                    token);

                var items = result.Items
                    .Select(MapToResponse)
                    .ToList();

                return new PagedResponse<CategoryResponse>
                {
                    Items = items,
                    TotalCount = result.TotalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };
            },
            options: CacheOptions,
            tags:
            [
                CategoryCacheConstants.AllCategoriesTag,
                CategoryCacheConstants.CategoryListsTag
            ],
            cancellationToken: cancellationToken);
    }

    public async ValueTask<CategoryResponse?> GetByIdAsync(
        int categoryId,
        CancellationToken cancellationToken)
    {
        var cacheKey = CategoryCacheConstants.GetDetailsKey(categoryId);

        return await _hybridCache.GetOrCreateAsync<CategoryResponse?>(
            cacheKey,
            async token =>
            {
                var category = await _categoryRepository.GetByIdAsync(
                    categoryId,
                    token);

                return category is null
                    ? null
                    : MapToResponse(category);
            },
            options: CacheOptions,
            tags:
            [
                CategoryCacheConstants.AllCategoriesTag,
                CategoryCacheConstants.GetCategoryTag(categoryId)
            ],
            cancellationToken: cancellationToken);
    }

    public async ValueTask InvalidateListsAsync(
        CancellationToken cancellationToken)
    {
        await _hybridCache.RemoveByTagAsync(
            CategoryCacheConstants.CategoryListsTag,
            cancellationToken);
    }

    public async ValueTask InvalidateCategoryAsync(
        int categoryId,
        CancellationToken cancellationToken)
    {
        await _hybridCache.RemoveByTagAsync(
            CategoryCacheConstants.GetCategoryTag(categoryId),
            cancellationToken);

        await InvalidateListsAsync(cancellationToken);
    }

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