using Feryal.Application.DTOs.Wishlist;

namespace Feryal.Application.Interfaces;

public interface IWishlistCacheService
{
    Task<WishlistItemsPageResponse> GetItemsAsync(
        int userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task InvalidateUserAsync(
        int userId,
        CancellationToken cancellationToken);

    Task InvalidateAllAsync(
        CancellationToken cancellationToken);
}