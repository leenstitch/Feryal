namespace Feryal.Infrastructure.Caching.Wishlist;

public static class WishlistCacheConstants
{
    public const string AllWishlistsTag = "wishlists:all";

    public static string GetUserTag(int userId)
        => $"wishlist:user:{userId}";

    public static string GetItemsKey(
        int userId,
        int pageNumber,
        int pageSize)
        => $"wishlist:user:{userId}:items:{pageNumber}:{pageSize}";
}