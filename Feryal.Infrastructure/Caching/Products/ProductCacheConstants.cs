using System.Security.Cryptography;
using System.Text;
using Feryal.Application.DTOs.Products;

namespace Feryal.Infrastructure.Caching.Products;

// Defines shared cache tags and cache key generators for product caching operations.
public static class ProductCacheConstants
{
    // Tag used to group product list cache entries for targeted invalidation.
    public const string ProductListsTag = "products:lists";

    // Tag used to identify all product-related cache entries for global invalidation.
    public const string AllProductsTag = "products:all";

    // Generates a cache tag associated with a specific product.
    public static string GetProductTag(int productId) =>
        $"product:{productId}";

    // Builds a cache key for the details of a specific product.
    public static string GetDetailsKey(int productId) =>
        $"products:details:{productId}";

    // Generates a unique cache key based on the product query parameters.
    public static string GetListKey(ProductQueryParameters parameters)
    {
        // Combine pagination, filtering, and normalized search values into one string.
        var input = string.Join(
            "|",
            parameters.PageNumber,
            parameters.PageSize,
            parameters.CategoryId,
            parameters.ProductGroupId,
            parameters.Search?.Trim().ToLowerInvariant() ?? string.Empty);

        // Hash the combined input to create a consistent, compact cache key component.
        var hash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(input)));

        // Return the cache key containing the hash of the query parameters.
        return $"products:list:{hash}";
    }
}