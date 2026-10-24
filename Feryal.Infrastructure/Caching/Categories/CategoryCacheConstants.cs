
namespace Feryal.Infrastructure.Caching.Categories;

public static class CategoryCacheConstants
{
    // Cache keys
    public const string ListKeyPrefix = "categories:list";
    public const string DetailsKeyPrefix = "categories:details";

    // Cache tags
    public const string AllCategoriesTag = "categories:all";
    public const string CategoryListsTag = "categories:lists";

    public static string GetListKey(
        int pageNumber,
        int pageSize)
    {
        return $"{ListKeyPrefix}:page:{pageNumber}:size:{pageSize}";
    }

    public static string GetDetailsKey(int categoryId)
    {
        return $"{DetailsKeyPrefix}:{categoryId}";
    }

    public static string GetCategoryTag(int categoryId)
    {
        return $"category:{categoryId}";
    }
}