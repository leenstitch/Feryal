
namespace Feryal.Infrastructure.Caching.ProductGroups;

public static class ProductGroupCacheConstants
{
    public const string ListKeyPrefix = "productgroups:list";
    public const string DetailsKeyPrefix = "productgroups:details";

    public const string ProductGroupListsTag = "productgroups:lists";

    public static string GetListKey(int pageNumber, int pageSize)
        => $"{ListKeyPrefix}:page:{pageNumber}:size:{pageSize}";

    public static string GetDetailsKey(int productGroupId)
        => $"{DetailsKeyPrefix}:{productGroupId}";

    public static string GetProductGroupTag(int productGroupId)
        => $"productgroup:{productGroupId}";
}