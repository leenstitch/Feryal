using Feryal.Application.DTOs.Common;
using Feryal.Application.DTOs.ProductGroups;

// Defines the contract for caching product group data and invalidating cached information.
public interface IProductGroupCacheService
{
    // Retrieves a paginated list of product groups from the cache.
    ValueTask<PagedResponse<ProductGroupResponse>> GetAllAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    // Retrieves a product group by its ID from the cache, if available.
    ValueTask<ProductGroupResponse?> GetByIdAsync(
        int productGroupId,
        CancellationToken cancellationToken);

    // Invalidates cached product group lists.
    ValueTask InvalidateListsAsync(
        CancellationToken cancellationToken);

    // Invalidates the cached data for a specific product group.
    ValueTask InvalidateProductGroupAsync(
        int productGroupId,
        CancellationToken cancellationToken);
}