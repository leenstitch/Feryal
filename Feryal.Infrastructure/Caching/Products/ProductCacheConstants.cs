using System.Security.Cryptography;
using System.Text;
using Feryal.Application.DTOs.Products;


namespace Feryal.Infrastructure.Caching.Products;

public static class ProductCacheConstants
{
    public const string ProductListsTag = "products:lists";
    public const string AllProductsTag = "products:all";

    public static string GetProductTag(int productId) =>
        $"product:{productId}";

    public static string GetDetailsKey(int productId) =>
        $"products:details:{productId}";

    public static string GetListKey(ProductQueryParameters parameters)
    {
        var input = string.Join(
            "|",
            parameters.PageNumber,
            parameters.PageSize,
            parameters.CategoryId,
            parameters.ProductGroupId,
            parameters.Search?.Trim().ToLowerInvariant() ?? string.Empty);

        var hash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(input)));

        return $"products:list:{hash}";
    }
}