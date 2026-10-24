using Feryal.Application.DTOs.Wishlist;

namespace Feryal.Application.Interfaces;

// Defines the contract for wishlist-related business operations.
public interface IWishlistService
{
    // Retrieves the user's paginated wishlist items and pagination details.
    Task<WishlistItemsPageResponse> GetItemsAsync(
        int userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    // Adds a product variant to the user's wishlist.
    Task AddItemAsync(
        int userId,
        int productVariantId,
        CancellationToken cancellationToken);

    // Removes a product variant from the user's wishlist.
    Task RemoveItemAsync(
        int userId,
        int productVariantId,
        CancellationToken cancellationToken);
}