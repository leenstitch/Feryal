using Feryal.Application.DTOs.Cart;

// Defines the contract for caching cart data and invalidating cached user cart information.
public interface ICartCacheService
{
    // Retrieves the user's paginated cart items from the cache.
    Task<CartPageResponse> GetItemsAsync(
        int userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    // Invalidates the cached cart data associated with the specified user.
    Task InvalidateUserAsync(
        int userId,
        CancellationToken cancellationToken);
}