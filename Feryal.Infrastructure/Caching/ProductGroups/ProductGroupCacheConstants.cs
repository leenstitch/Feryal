namespace Feryal.Infrastructure.Caching.ProductGroups;

// Defines shared cache keys and tags for product group caching operations.
public static class ProductGroupCacheConstants
{
    // Prefix used to identify cached product group list entries.
    public const string ListKeyPrefix = "productgroups:list";

    // Prefix used to identify cached product group details entries.
    public const string DetailsKeyPrefix = "productgroups:details";

    // Tag used to group product group list entries for targeted invalidation.
    public const string ProductGroupListsTag = "productgroups:lists";

    // Builds a unique cache key for a paginated product group list.
    public static string GetListKey(int pageNumber, int pageSize)
        => $"{ListKeyPrefix}:page:{pageNumber}:size:{pageSize}";

    // Builds a cache key for the details of a specific product group.
    public static string GetDetailsKey(int productGroupId)
        => $"{DetailsKeyPrefix}:{productGroupId}";

    // Generates a tag for cache entries associated with a specific product group.
    public static string GetProductGroupTag(int productGroupId)
        => $"productgroup:{productGroupId}";
}