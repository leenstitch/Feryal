using Feryal.Application.DTOs.Wishlist;

namespace Feryal.Application.Interfaces;

public interface IWishlistService
{
    Task<WishlistItemsPageResponse> GetItemsAsync(
        int userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task AddItemAsync(
        int userId,
        int productVariantId,
        CancellationToken cancellationToken);

    Task RemoveItemAsync(
        int userId,
        int productVariantId,
        CancellationToken cancellationToken);
}