using Feryal.Application.DTOs.Common;
using Feryal.Application.DTOs.Products;
using Feryal.Application.DTOs.Products.Response;

// Defines the contract for caching product data and invalidating cached product information.
public interface IProductCacheService
{
    // Retrieves a paginated list of products from the cache using the specified query parameters.
    ValueTask<PagedResponse<ProductListResponse>> GetAllAsync(
        ProductQueryParameters parameters,
        CancellationToken cancellationToken);

    // Retrieves a product by its ID from the cache, if available.
    ValueTask<ProductResponse?> GetByIdAsync(
        int productId,
        CancellationToken cancellationToken);

    // Invalidates cached product lists.
    ValueTask InvalidateListsAsync(
        CancellationToken cancellationToken);

    // Invalidates the cached data for a specific product.
    ValueTask InvalidateProductAsync(
        int productId,
        CancellationToken cancellationToken);
}