namespace Feryal.Infrastructure.Caching.Wishlist;

// Defines shared cache tags and cache key generators for wishlist caching operations.
public static class WishlistCacheConstants
{
    // Tag used to group all wishlist cache entries for global invalidation.
    public const string AllWishlistsTag = "wishlists:all";

    // Generates a cache tag associated with a specific user's wishlist.
    public static string GetUserTag(int userId)
        => $"wishlist:user:{userId}";

    // Builds a unique cache key for a user's wishlist items and pagination settings.
    public static string GetItemsKey(
        int userId,
        int pageNumber,
        int pageSize)
        => $"wishlist:user:{userId}:items:{pageNumber}:{pageSize}";
}