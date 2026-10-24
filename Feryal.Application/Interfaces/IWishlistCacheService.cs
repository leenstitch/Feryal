using Feryal.Application.DTOs.Wishlist;

namespace Feryal.Application.Interfaces;

// Defines the contract for caching wishlist data and invalidating cached wishlist information.
public interface IWishlistCacheService
{
    // Retrieves the user's paginated wishlist items from the cache.
    Task<WishlistItemsPageResponse> GetItemsAsync(
        int userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    // Invalidates the cached wishlist data associated with the specified user.
    Task InvalidateUserAsync(
        int userId,
        CancellationToken cancellationToken);

    // Invalidates all cached wishlist data.
    Task InvalidateAllAsync(
        CancellationToken cancellationToken);
}