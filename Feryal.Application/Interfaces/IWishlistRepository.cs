using Feryal.Application.DTOs.Wishlist;
using WishlistEntity = Feryal.Domain.Entities.Wishlist;
using Feryal.Domain.Entities;

namespace Feryal.Application.Interfaces;

// Defines the contract for database operations related to wishlists and their items.
public interface IWishlistRepository
{
    // Retrieves the user's wishlist for update operations.
    Task<WishlistEntity?> GetByUserIdForUpdateAsync(
        int userId,
        CancellationToken cancellationToken);

    // Retrieves a product variant with the information required to add it to a wishlist.
    Task<ProductVariant?> GetVariantForWishlistAsync(
        int productVariantId,
        CancellationToken cancellationToken);

    // Checks whether a product variant already exists in the specified wishlist.
    Task<bool> ContainsVariantAsync(
        int wishlistId,
        int productVariantId,
        CancellationToken cancellationToken);

    // Retrieves paginated visible wishlist items for the specified user, along with the total count.
    Task<(List<WishlistItemResponse> Items, int TotalCount)>
        GetPagedVisibleItemsAsync(
            int userId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken);

    // Adds a wishlist to the database.
    Task AddWishlistAsync(
        WishlistEntity? wishlist,
        CancellationToken cancellationToken);

    // Adds an item to the wishlist.
    Task AddItemAsync(
        WishlistItem item,
        CancellationToken cancellationToken);

    // Removes an item from the wishlist.
    Task RemoveItemAsync(
        WishlistItem item,
        CancellationToken cancellationToken);
}