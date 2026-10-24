namespace Feryal.Infrastructure.Caching.Categories;

// Defines shared cache keys and tags for category caching operations.
public static class CategoryCacheConstants
{
    // Cache keys
    // Prefix used to identify cached category list entries.
    public const string ListKeyPrefix = "categories:list";

    // Prefix used to identify cached category details entries.
    public const string DetailsKeyPrefix = "categories:details";

    // Cache tags
    // Tag shared by all category cache entries for global invalidation.
    public const string AllCategoriesTag = "categories:all";

    // Tag used to group category list cache entries for targeted invalidation.
    public const string CategoryListsTag = "categories:lists";

    // Builds a unique cache key for a paginated category list.
    public static string GetListKey(
        int pageNumber,
        int pageSize)
    {
        return $"{ListKeyPrefix}:page:{pageNumber}:size:{pageSize}";
    }

    // Builds a cache key for the details of a specific category.
    public static string GetDetailsKey(int categoryId)
    {
        return $"{DetailsKeyPrefix}:{categoryId}";
    }

    // Generates a tag that identifies cache entries associated with a specific category.
    public static string GetCategoryTag(int categoryId)
    {
        return $"category:{categoryId}";
    }
}