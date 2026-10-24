namespace Feryal.Application.Events.Wishlist;

public sealed record WishlistCacheInvalidationRequested(
    int? UserId);