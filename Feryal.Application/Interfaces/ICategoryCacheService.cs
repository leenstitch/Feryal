using Feryal.Application.DTOs.Categories;
using Feryal.Application.DTOs.Common;

// Defines the contract for caching category data and invalidating cached category information.
public interface ICategoryCacheService
{
    // Retrieves paginated category data from the cache.
    ValueTask<PagedResponse<CategoryResponse>> GetAllAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    // Retrieves a category by its ID from the cache, if available.
    ValueTask<CategoryResponse?> GetByIdAsync(
        int categoryId,
        CancellationToken cancellationToken);

    // Invalidates cached category lists.
    ValueTask InvalidateListsAsync(
        CancellationToken cancellationToken);

    // Invalidates the cached data for a specific category.
    ValueTask InvalidateCategoryAsync(
        int categoryId,
        CancellationToken cancellationToken);
}